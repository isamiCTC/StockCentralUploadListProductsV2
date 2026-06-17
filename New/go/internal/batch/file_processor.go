package batch

import (
	"context"
	"fmt"
	"slices"
	"strings"
	"sync"
	"time"

	"stockcentraluploadlistproductsv2/internal/catalog"
	"stockcentraluploadlistproductsv2/internal/domain"
	"stockcentraluploadlistproductsv2/internal/excel"
	"stockcentraluploadlistproductsv2/internal/files"
	"stockcentraluploadlistproductsv2/internal/images"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/notifications"
	"stockcentraluploadlistproductsv2/internal/products"
	"stockcentraluploadlistproductsv2/internal/results"
)

// Este archivo modela el procesamiento de un archivo individual.
//
// Responsabilidades actuales:
// - preparar rutas
// - mover el archivo a `processing`
// - leer y validar el Excel
// - mapear filas
// - procesar filas con worker pool
// - notificar el resultado final del archivo
// - mover el original a `processed`
// - escribir el Excel de `Resultados` o `ErroresEstructura`
type FileProcessor struct {
	rowWorkers      int
	rowTimeout      time.Duration
	syncImages      bool
	catalogResolver *catalog.Resolver
	excelReader     *excel.Reader
	imageDownloader *images.Downloader
	productsClient  *products.Client
	mover           *files.Mover
	notifier        *notifications.Service
	resultsWriter   *results.Writer
	logs            logging.LoggerSet
}

// NewFileProcessor construye el procesador de archivos individuales.
func NewFileProcessor(
	rowWorkers int,
	rowTimeout time.Duration,
	syncImages bool,
	catalogResolver *catalog.Resolver,
	excelReader *excel.Reader,
	imageDownloader *images.Downloader,
	productsClient *products.Client,
	mover *files.Mover,
	notifier *notifications.Service,
	resultsWriter *results.Writer,
	logs logging.LoggerSet,
) *FileProcessor {
	if rowWorkers < 1 {
		rowWorkers = 1
	}
	if rowTimeout <= 0 {
		rowTimeout = 120 * time.Second
	}

	return &FileProcessor{
		rowWorkers:      rowWorkers,
		rowTimeout:      rowTimeout,
		syncImages:      syncImages,
		catalogResolver: catalogResolver,
		excelReader:     excelReader,
		imageDownloader: imageDownloader,
		productsClient:  productsClient,
		mover:           mover,
		notifier:        notifier,
		resultsWriter:   resultsWriter,
		logs:            logs,
	}
}

// Process ejecuta el flujo completo de un archivo.
func (p *FileProcessor) Process(ctx context.Context, job domain.FileJob) (domain.FileResult, error) {
	startedAt := time.Now()
	job = p.mover.BuildPaths(job)

	p.logs.Summary.Info("file-start",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.RelativePath),
	)

	p.logs.Detail.Info("file-processing-started",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.InputPath),
	)

	job, err := p.mover.MoveToProcessing(job)
	if err != nil {
		return domain.FileResult{
			ProviderID:    job.ProviderID,
			InputPath:     job.InputPath,
			Status:        domain.FileStatusFailed,
			StartedAt:     startedAt,
			FinishedAt:    time.Now(),
			FailureReason: err.Error(),
		}, err
	}

	p.logs.Detail.Info("file-moved-to-processing",
		logging.Int("provider_id", job.ProviderID),
		logging.String("processing_path", job.ProcessingPath),
	)

	workbook, err := p.excelReader.Read(job.ProcessingPath)
	if err != nil {
		return domain.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         domain.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	if !workbook.IsStructureValid() {
		return p.finishWithStructureErrors(ctx, startedAt, job, workbook)
	}

	p.logs.Detail.Info("excel-validated",
		logging.Int("provider_id", job.ProviderID),
		logging.String("sheet_name", workbook.SheetName),
		logging.String("format", string(workbook.Format)),
		logging.Int("rows_detected", countNonEmptyRows(workbook.Rows)),
	)

	mappedRows, err := excel.MapRows(workbook)
	if err != nil {
		return domain.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         domain.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	skippedRows, validRows, mappingErrorRows := summarizeMappedRows(mappedRows)
	p.logs.Detail.Info("excel-rows-mapped",
		logging.Int("provider_id", job.ProviderID),
		logging.Int("valid_rows", validRows),
		logging.Int("error_rows", mappingErrorRows),
		logging.Int("skipped_rows", skippedRows),
	)

	rowResults, err := p.processMappedRows(ctx, job.ProviderID, workbook.Format, mappedRows)
	if err != nil {
		return domain.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         domain.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	job, err = p.mover.MoveToProcessed(job)
	if err != nil {
		return domain.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         domain.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	if err := p.resultsWriter.WriteRowResults(job.ResultsPath, rowResults); err != nil {
		return domain.FileResult{
			ProviderID:      job.ProviderID,
			InputPath:       job.InputPath,
			ProcessingPath:  job.ProcessingPath,
			ProcessedPath:   job.ProcessedPath,
			Status:          domain.FileStatusFailed,
			StartedAt:       startedAt,
			FinishedAt:      time.Now(),
			FailureReason:   err.Error(),
			ResultsFilePath: job.ResultsPath,
		}, fmt.Errorf("write row results workbook: %w", err)
	}

	processedRows, successfulRows, partialRows, errorRows := summarizeRowResults(rowResults)
	result := domain.FileResult{
		ProviderID:          job.ProviderID,
		InputPath:           job.InputPath,
		ProcessingPath:      job.ProcessingPath,
		ProcessedPath:       job.ProcessedPath,
		Status:              resolveFileStatus(errorRows, partialRows),
		DetectedRows:        countNonEmptyRows(workbook.Rows),
		ProcessedRows:       processedRows,
		SkippedRows:         skippedRows,
		SuccessfulRows:      successfulRows,
		PartialRows:         partialRows,
		ErrorRows:           errorRows,
		StartedAt:           startedAt,
		FinishedAt:          time.Now(),
		ResultsFilePath:     job.ResultsPath,
		StructureErrorsPath: "",
		RowResults:          rowResults,
	}

	p.logs.Summary.Info("file-finished",
		logging.Int("provider_id", result.ProviderID),
		logging.String("status", string(result.Status)),
		logging.Int("detected_rows", result.DetectedRows),
		logging.Int("processed_rows", result.ProcessedRows),
		logging.Int("successful_rows", result.SuccessfulRows),
		logging.Int("partial_rows", result.PartialRows),
		logging.Int("error_rows", result.ErrorRows),
		logging.Int("skipped_rows", result.SkippedRows),
		logging.String("processed_path", result.ProcessedPath),
	)

	p.logs.Detail.Info("file-processing-finished",
		logging.Int("provider_id", result.ProviderID),
		logging.String("status", string(result.Status)),
		logging.String("processed_path", result.ProcessedPath),
		logging.String("results_path", result.ResultsFilePath),
	)

	p.notifyFileProcessed(ctx, job, result)

	return result, nil
}

// finishWithStructureErrors cierra el flujo especial cuando el layout del
// Excel es inválido y se debe generar `ErroresEstructura`.
func (p *FileProcessor) finishWithStructureErrors(ctx context.Context, startedAt time.Time, job domain.FileJob, workbook excel.Workbook) (domain.FileResult, error) {
	for _, structureError := range workbook.StructureErrors {
		p.logs.Detail.Error("excel-structure-error",
			logging.Int("provider_id", job.ProviderID),
			logging.String("field", structureError.Field),
			logging.String("message", structureError.Message),
			logging.String("detail", structureError.Detail),
		)
	}

	job, moveErr := p.mover.MoveToProcessed(job)
	if moveErr != nil {
		return domain.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         domain.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  moveErr.Error(),
		}, moveErr
	}

	if err := p.resultsWriter.WriteStructureErrors(job.StructureErrPath, workbook.StructureErrors); err != nil {
		return domain.FileResult{
			ProviderID:          job.ProviderID,
			InputPath:           job.InputPath,
			ProcessingPath:      job.ProcessingPath,
			ProcessedPath:       job.ProcessedPath,
			Status:              domain.FileStatusFailed,
			StartedAt:           startedAt,
			FinishedAt:          time.Now(),
			FailureReason:       err.Error(),
			StructureErrorsPath: job.StructureErrPath,
		}, fmt.Errorf("write structure errors workbook: %w", err)
	}

	result := domain.FileResult{
		ProviderID:          job.ProviderID,
		InputPath:           job.InputPath,
		ProcessingPath:      job.ProcessingPath,
		ProcessedPath:       job.ProcessedPath,
		Status:              domain.FileStatusStructureError,
		DetectedRows:        countNonEmptyRows(workbook.Rows),
		StartedAt:           startedAt,
		FinishedAt:          time.Now(),
		FailureReason:       "excel structure validation failed",
		StructureErrorsPath: job.StructureErrPath,
	}

	p.logs.Summary.Warn("file-structure-error",
		logging.Int("provider_id", result.ProviderID),
		logging.String("file", job.RelativePath),
		logging.String("status", string(result.Status)),
		logging.String("structure_errors_path", result.StructureErrorsPath),
	)

	p.notifyFileProcessed(ctx, job, result)

	return result, nil
}

// notifyFileProcessed dispara la notificación sin afectar el resultado del
// archivo si SendGrid falla.
func (p *FileProcessor) notifyFileProcessed(ctx context.Context, job domain.FileJob, result domain.FileResult) {
	if p.notifier == nil {
		return
	}

	if err := p.notifier.NotifyFileProcessed(ctx, job, result); err != nil {
		p.logs.Summary.Error("notification-failed",
			logging.Int("provider_id", job.ProviderID),
			logging.String("file", job.RelativePath),
			logging.String("error", err.Error()),
		)
		p.logs.Detail.Error("notification-failed",
			logging.Int("provider_id", job.ProviderID),
			logging.String("file", job.RelativePath),
			logging.String("error", err.Error()),
		)
	}
}

// countNonEmptyRows cuenta solo filas con contenido real.
func countNonEmptyRows(rows []excel.RawRow) int {
	count := 0
	for _, row := range rows {
		if row.IsEmpty {
			continue
		}
		count++
	}
	return count
}

// summarizeMappedRows consolida cuántas filas quedaron vacías, válidas o con error.
func summarizeMappedRows(rows []excel.MappedRow) (skippedRows, validRows, errorRows int) {
	for _, row := range rows {
		if row.IsEmpty {
			skippedRows++
			continue
		}
		if row.HasErrors() {
			errorRows++
			continue
		}
		validRows++
	}

	return skippedRows, validRows, errorRows
}

// summarizeRowResults calcula las métricas finales del archivo a partir de
// los resultados devueltos por cada fila.
func summarizeRowResults(rows []domain.RowResult) (processedRows, successfulRows, partialRows, errorRows int) {
	for _, row := range rows {
		switch row.Status {
		case domain.RowStatusSkipped:
			continue
		case domain.RowStatusError:
			processedRows++
			errorRows++
		case domain.RowStatusPartialOK:
			processedRows++
			partialRows++
		case domain.RowStatusOK:
			processedRows++
			successfulRows++
		}
	}

	return processedRows, successfulRows, partialRows, errorRows
}

// resolveFileStatus decide el estado final del archivo.
func resolveFileStatus(errorRows, partialRows int) domain.FileStatus {
	if errorRows > 0 || partialRows > 0 {
		return domain.FileStatusProcessedErrors
	}

	return domain.FileStatusProcessed
}

// processMappedRows procesa filas con un worker pool fijo y devuelve los
// resultados ordenados por número de fila para escribir luego el Excel.
func (p *FileProcessor) processMappedRows(ctx context.Context, providerID int, format excel.FileFormat, rows []excel.MappedRow) ([]domain.RowResult, error) {
	jobs := make(chan excel.MappedRow)
	resultsChan := make(chan domain.RowResult, len(rows))

	var wg sync.WaitGroup
	for workerIndex := 0; workerIndex < p.rowWorkers; workerIndex++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for row := range jobs {
				rowCtx, cancel := context.WithTimeout(ctx, p.rowTimeout)
				result := p.processSingleRow(rowCtx, providerID, format, row)
				cancel()
				resultsChan <- result
			}
		}()
	}

	go func() {
		defer close(jobs)
		for _, row := range rows {
			select {
			case <-ctx.Done():
				return
			case jobs <- row:
			}
		}
	}()

	go func() {
		wg.Wait()
		close(resultsChan)
	}()

	results := make([]domain.RowResult, 0, len(rows))
	for result := range resultsChan {
		results = append(results, result)
	}

	if err := ctx.Err(); err != nil {
		return nil, err
	}

	slices.SortFunc(results, func(a, b domain.RowResult) int {
		switch {
		case a.ExcelRowNumber < b.ExcelRowNumber:
			return -1
		case a.ExcelRowNumber > b.ExcelRowNumber:
			return 1
		default:
			return 0
		}
	})

	return results, nil
}

// processSingleRow trata una fila como una transacción lógica completa.
func (p *FileProcessor) processSingleRow(ctx context.Context, providerID int, format excel.FileFormat, row excel.MappedRow) domain.RowResult {
	if row.IsEmpty {
		return domain.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         domain.RowStatusSkipped,
			Message:        "Fila vacía",
			Detail:         "La fila no contiene valores útiles y se omite.",
		}
	}

	if row.HasErrors() {
		detail := joinRowIssues(row.Issues)
		p.logs.Detail.Error("row-mapping-error",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("detail", detail),
		)
		return domain.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         domain.RowStatusError,
			Message:        "La fila no pasó las validaciones previas",
			Detail:         detail,
		}
	}

	p.logs.Detail.Info(fmt.Sprintf("-------- SKU: %s ----------", row.SKU),
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
	)

	switch format {
	case excel.FileFormatStockUpdate:
		return p.processStockUpdateRow(ctx, providerID, row)
	case excel.FileFormatFullImport:
		return p.processFullImportRow(ctx, providerID, row)
	default:
		return domain.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         domain.RowStatusError,
			Message:        "Formato de archivo no soportado",
			Detail:         fmt.Sprintf("Formato recibido: %s", format),
		}
	}
}

// processStockUpdateRow implementa el caso reducido de SKU + STOCK.
func (p *FileProcessor) processStockUpdateRow(ctx context.Context, providerID int, row excel.MappedRow) domain.RowResult {
	result := baseRowResult(providerID, row)
	result.ProductResult = "NO_PROCESADO"
	result.ImagesResult = "NO_APLICA"
	result.Message = "Actualización de stock completada"

	p.logs.Detail.Info("validation-stock-row-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
	)

	if row.StockUpdate == nil {
		result.Status = domain.RowStatusError
		result.Message = "Fila sin payload de stock"
		result.Detail = "La fila pasó por el worker sin DTO de stock."
		return result
	}

	updateMeta, err := p.productsClient.SyncStockLegacy(ctx, providerID, row.StockUpdate.SKU, row.StockUpdate.Stock)
	if err != nil {
		result.Status = domain.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "Falló la actualización de stock en la API", err)
		p.logs.Detail.Error("stock-sync-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	result.Status = domain.RowStatusOK
	result.ProductResult = "ACTUALIZADO"
	result.Detail = describeMeta(updateMeta)

	p.logs.Detail.Info("stock-sync-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("api", describeMeta(updateMeta)),
	)

	return result
}

// processFullImportRow implementa la importación completa de 19 columnas.
func (p *FileProcessor) processFullImportRow(ctx context.Context, providerID int, row excel.MappedRow) domain.RowResult {
	result := baseRowResult(providerID, row)
	result.ProductResult = "NO_PROCESADO"
	result.ImagesResult = "NO_APLICA"

	p.logs.Detail.Info("validation-full-row-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
	)

	if row.FullImport == nil {
		result.Status = domain.RowStatusError
		result.Message = "Fila sin payload completo"
		result.Detail = "La fila pasó por el worker sin DTO de importación."
		return result
	}

	resolution, err := p.catalogResolver.ResolveBySubcategory(ctx, providerID, row.FullImport.SubCategory)
	if err != nil {
		result.Status = domain.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "No se pudo resolver la subcategoría", err)
		p.logs.Detail.Error("subcategory-resolution-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	p.logs.Detail.Info("subcategory-resolution-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("source", string(resolution.Source)),
		logging.String("category_code", resolution.Branch.Code),
		logging.String("category_name", resolution.Branch.Name),
	)

	input := products.ProductInput{
		SKU:              row.FullImport.SKU,
		Name:             row.FullImport.Name,
		Brand:            row.FullImport.Brand,
		Description:      row.FullImport.Description,
		ShortDescription: row.FullImport.ShortDescription,
		Stock:            row.FullImport.Stock,
		Price:            row.FullImport.Price,
		ListPrice:        row.FullImport.ListPrice,
		NetPrice:         row.FullImport.NetPrice,
		Taxes:            row.FullImport.Taxes,
		Height:           row.FullImport.Height,
		Width:            row.FullImport.Width,
		Depth:            row.FullImport.Depth,
		WeightKilograms:  row.FullImport.WeightKilograms,
	}

	product := p.productsClient.BuildProductFromInput(providerID, input, resolution.Branch)
	upsertResult, err := p.productsClient.UpsertProductLegacy(ctx, providerID, product)
	if err != nil {
		result.Status = domain.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "Falló el alta o actualización del producto", err)
		p.logs.Detail.Error("product-upsert-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	result.ProductResult = translateProductAction(upsertResult.Action)
	p.logs.Detail.Info("product-upsert-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("action", upsertResult.Action),
	)

	if !p.syncImages {
		result.Status = domain.RowStatusOK
		result.Message = "Producto impactado sin sincronización de imágenes"
		result.Detail = "La sincronizacion global de imagenes esta desactivada por configuracion."
		result.ImagesResult = "NO_APLICA"
		return result
	}

	if !row.FullImport.SyncImages {
		result.Status = domain.RowStatusOK
		result.Message = "Producto impactado sin sincronización de imágenes"
		result.Detail = "La fila quedó OK y no trajo URLs válidas de imágenes."
		result.ImagesResult = "NO_APLICA"
		return result
	}

	imagesSynced, imagesFailed, imageDetails := p.syncRowImages(ctx, providerID, row)

	if ctx.Err() != nil {
		result.Status = domain.RowStatusPartialOK
		result.ImagesResult = "PARCIAL"
		result.Message, result.Detail = classifyRowTimeoutWhileImages(ctx, imagesSynced, imagesFailed, imageDetails)
		return result
	}

	if imagesFailed > 0 {
		result.Status = domain.RowStatusPartialOK
		result.ImagesResult = "PARCIAL"
		result.Message = "Producto impactado con errores en imágenes"
		result.Detail = strings.Join(imageDetails, " | ")
		return result
	}

	result.Status = domain.RowStatusOK
	result.ImagesResult = "OK"
	result.Message = "Producto e imágenes impactados correctamente"
	result.Detail = fmt.Sprintf("Imagenes sincronizadas correctamente: %d", imagesSynced)
	return result
}

// syncRowImages procesa las imágenes de una fila y devuelve métricas útiles
// para el Excel final de resultados.
func (p *FileProcessor) syncRowImages(ctx context.Context, providerID int, row excel.MappedRow) (imagesSynced, imagesFailed int, details []string) {
	if row.FullImport == nil || len(row.FullImport.ImageURLs) == 0 {
		return 0, 0, nil
	}

	for index, imageURL := range row.FullImport.ImageURLs {
		if err := ctx.Err(); err != nil {
			details = append(details, fmt.Sprintf("Procesamiento de imagenes interrumpido: %s", err.Error()))
			break
		}

		p.logs.Detail.Info("image-sync-start",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.Int("image_index", index),
			logging.String("url", imageURL),
		)

		base64Image, err := p.imageDownloader.DownloadAsBase64(ctx, imageURL)
		if err != nil {
			if ctx.Err() != nil {
				details = append(details, fmt.Sprintf("Imagen %d: timeout o cancelacion: %s", index, ctx.Err().Error()))
				break
			}
			imagesFailed++
			details = append(details, fmt.Sprintf("Imagen %d: fallo descarga: %s", index, err.Error()))
			p.logs.Detail.Error("image-download-failed",
				logging.Int("provider_id", providerID),
				logging.Int("excel_row", row.ExcelRowNumber),
				logging.String("sku", row.SKU),
				logging.Int("image_index", index),
				logging.String("error", err.Error()),
			)
			continue
		}

		syncResult, err := p.productsClient.SyncImageLegacy(ctx, providerID, row.SKU, index, base64Image)
		if err != nil {
			if ctx.Err() != nil {
				details = append(details, fmt.Sprintf("Imagen %d: timeout o cancelacion: %s", index, ctx.Err().Error()))
				break
			}
			imagesFailed++
			details = append(details, fmt.Sprintf("Imagen %d: fallo sync API: %s", index, err.Error()))
			p.logs.Detail.Error("image-sync-failed",
				logging.Int("provider_id", providerID),
				logging.Int("excel_row", row.ExcelRowNumber),
				logging.String("sku", row.SKU),
				logging.Int("image_index", index),
				logging.String("error", err.Error()),
			)
			continue
		}

		imagesSynced++
		if syncResult.Action == "SKIP_SAME_IMAGE" {
			details = append(details, fmt.Sprintf("Imagen %d: sin cambios, no se vuelve a subir", index))
		}
		p.logs.Detail.Info("image-sync-ok",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.Int("image_index", index),
			logging.String("action", syncResult.Action),
		)
	}

	return imagesSynced, imagesFailed, details
}

// baseRowResult arma la estructura común para cualquier resultado de fila.
func baseRowResult(providerID int, row excel.MappedRow) domain.RowResult {
	return domain.RowResult{
		ProviderID:     providerID,
		ExcelRowNumber: row.ExcelRowNumber,
		SKU:            row.SKU,
	}
}

// joinRowIssues convierte los problemas del mapper en un texto entendible.
func joinRowIssues(issues []excel.RowIssue) string {
	parts := make([]string, 0, len(issues))
	for _, issue := range issues {
		piece := fmt.Sprintf("%s: %s", issue.Field, issue.Message)
		if strings.TrimSpace(issue.Detail) != "" {
			piece += " (" + strings.TrimSpace(issue.Detail) + ")"
		}
		parts = append(parts, piece)
	}
	return strings.Join(parts, " | ")
}

// translateProductAction convierte la acción técnica de la API a una etiqueta
// más humana para el Excel final.
func translateProductAction(action string) string {
	switch strings.ToUpper(strings.TrimSpace(action)) {
	case "CREATE":
		return "CREADO"
	case "UPDATE":
		return "ACTUALIZADO"
	default:
		return strings.ToUpper(strings.TrimSpace(action))
	}
}

// describeMeta resume de forma corta la respuesta HTTP útil para auditoría.
func describeMeta(meta interface{ GetStatusCode() int }) string {
	if meta == nil {
		return ""
	}
	return fmt.Sprintf("status=%d", meta.GetStatusCode())
}

func classifyRowError(ctx context.Context, baseMessage string, err error) (message, detail string) {
	if ctx.Err() == context.DeadlineExceeded {
		return "La fila excedió el timeout configurado", fmt.Sprintf("%s: %s", baseMessage, ctx.Err().Error())
	}
	if ctx.Err() == context.Canceled {
		return "La fila fue cancelada", fmt.Sprintf("%s: %s", baseMessage, ctx.Err().Error())
	}
	return baseMessage, err.Error()
}

func classifyRowTimeoutWhileImages(ctx context.Context, imagesSynced, imagesFailed int, details []string) (message, detail string) {
	if ctx.Err() == context.DeadlineExceeded {
		message = "Producto impactado pero la fila excedió el timeout durante imágenes"
	} else {
		message = "Producto impactado pero la fila fue cancelada durante imágenes"
	}

	parts := make([]string, 0, len(details)+2)
	parts = append(parts, fmt.Sprintf("Imagenes sincronizadas antes del corte: %d", imagesSynced))
	parts = append(parts, fmt.Sprintf("Imagenes fallidas antes del corte: %d", imagesFailed))
	parts = append(parts, details...)
	return message, strings.Join(parts, " | ")
}
