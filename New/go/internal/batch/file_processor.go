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
	// Guardamos el inicio para poder devolver duración y timestamps consistentes.
	startedAt := time.Now()

	// A partir del job base armamos todas las rutas derivadas:
	// processing, processed y archivos auxiliares de salida.
	job = p.mover.BuildPaths(job)

	// Summary deja una traza corta del archivo que arranca.
	p.logs.Summary.Info("file-start",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.RelativePath),
	)

	// Detail deja la ruta concreta para depuración más fina.
	p.logs.Detail.Info("file-processing-started",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.InputPath),
	)

	// Antes de leerlo, movemos el archivo a `processing` para separarlo
	// claramente de los archivos todavía pendientes.
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

	// La lectura del reader ya incluye validaciones estructurales básicas.
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

	// Si el layout del Excel no sirve, entramos en un flujo especial que
	// escribe `ErroresEstructura` en lugar de resultados por fila.
	if !workbook.IsStructureValid() {
		return p.finishWithStructureErrors(ctx, startedAt, job, workbook)
	}

	// A esta altura el archivo ya tiene estructura válida y se puede mapear.
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

	// Estas métricas describen cómo salió el mapeo antes de tocar negocio.
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

	// Recién cuando ya tenemos todos los resultados por fila movemos el
	// original a `processed`.
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

	// Este Excel es la salida principal para auditoría fila por fila.
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

	// Consolidamos métricas finales del archivo a partir del resultado de filas.
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

	// La notificación se intenta al final y nunca rompe el resultado del archivo.
	p.notifyFileProcessed(ctx, job, result)

	return result, nil
}

// finishWithStructureErrors cierra el flujo especial cuando el layout del
// Excel es inválido y se debe generar `ErroresEstructura`.
func (p *FileProcessor) finishWithStructureErrors(ctx context.Context, startedAt time.Time, job domain.FileJob, workbook excel.Workbook) (domain.FileResult, error) {
	// Registramos uno por uno los problemas estructurales encontrados.
	for _, structureError := range workbook.StructureErrors {
		p.logs.Detail.Error("excel-structure-error",
			logging.Int("provider_id", job.ProviderID),
			logging.String("field", structureError.Field),
			logging.String("message", structureError.Message),
			logging.String("detail", structureError.Detail),
		)
	}

	// Aunque el archivo sea inválido, igual lo sacamos de `processing`
	// para no reintentarlo como si siguiera pendiente.
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

	// En este escenario generamos una planilla de estructura en vez de una
	// planilla de resultados por fila.
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
	// Acá solo miramos el resultado del mapper, todavía sin negocio.
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
	// jobs reparte trabajo a los workers; resultsChan junta lo que devuelve
	// cada fila ya procesada.
	jobs := make(chan excel.MappedRow)
	resultsChan := make(chan domain.RowResult, len(rows))

	var wg sync.WaitGroup
	for workerIndex := 0; workerIndex < p.rowWorkers; workerIndex++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for row := range jobs {
				// Cada fila tiene su propio timeout para que una fila lenta no
				// bloquee indefinidamente al archivo entero.
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
			// Si el contexto global cayó, dejamos de mandar filas nuevas.
			case <-ctx.Done():
				return
			case jobs <- row:
			}
		}
	}()

	go func() {
		// Cerramos el canal de resultados recién cuando terminaron todos
		// los workers.
		wg.Wait()
		close(resultsChan)
	}()

	results := make([]domain.RowResult, 0, len(rows))
	for result := range resultsChan {
		// Los resultados llegan en orden no determinístico por la concurrencia.
		results = append(results, result)
	}

	if err := ctx.Err(); err != nil {
		return nil, err
	}

	// Reordenamos por número de fila para que el Excel final sea legible.
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
	// Las filas vacías no son error: simplemente no hacen nada.
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

	// Si el mapper ya marcó problemas, no entramos a negocio.
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

	// A partir de acá la fila está lista para ejecutar lógica real.
	p.logs.Detail.Info(fmt.Sprintf("-------- SKU: %s ----------", row.SKU),
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
	)

	// El formato define qué camino de negocio aplicar a esa fila.
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
	// Arrancamos con una base común y luego la vamos completando.
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

	// En stock update la única operación de negocio es sincronizar stock.
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

	// Si la API respondió bien, la fila queda totalmente OK.
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
	// Igual que en stock, partimos de un resultado base y lo enriquecemos.
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

	// Primero resolvemos la rama de catálogo que necesita el producto.
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

	// Convertimos la fila al modelo que espera la API de productos.
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

	// Si la sincronización global de imágenes está apagada, la fila termina acá.
	if !p.syncImages {
		result.Status = domain.RowStatusOK
		result.Message = "Producto impactado sin sincronización de imágenes"
		result.Detail = "La sincronizacion global de imagenes esta desactivada por configuracion."
		result.ImagesResult = "NO_APLICA"
		return result
	}

	// Si la fila no trajo imágenes válidas, también termina bien acá.
	if !row.FullImport.SyncImages {
		result.Status = domain.RowStatusOK
		result.Message = "Producto impactado sin sincronización de imágenes"
		result.Detail = "La fila quedó OK y no trajo URLs válidas de imágenes."
		result.ImagesResult = "NO_APLICA"
		return result
	}

	// Recién en este punto empezamos a tratar imágenes.
	imagesSynced, imagesFailed, imageDetails := p.syncRowImages(ctx, providerID, row)

	// Si el contexto venció durante imágenes, el producto ya pudo haber quedado
	// impactado, por eso devolvemos PARTIAL_OK.
	if ctx.Err() != nil {
		result.Status = domain.RowStatusPartialOK
		result.ImagesResult = "PARCIAL"
		result.Message, result.Detail = classifyRowTimeoutWhileImages(ctx, imagesSynced, imagesFailed, imageDetails)
		return result
	}

	// También queda parcial si alguna imagen falló aunque el producto se haya creado.
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
		// Antes de iniciar cada imagen chequeamos si la fila ya venció.
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

		// Paso 1: descargar la imagen remota y convertirla a base64.
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

		// Paso 2: enviar esa imagen a la API del producto.
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
		// La API puede decir que la imagen ya era igual y que no hizo falta subirla.
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

// classifyRowError traduce un error técnico a un mensaje de fila más claro,
// respetando si el contexto terminó por timeout o cancelación.
func classifyRowError(ctx context.Context, baseMessage string, err error) (message, detail string) {
	if ctx.Err() == context.DeadlineExceeded {
		return "La fila excedió el timeout configurado", fmt.Sprintf("%s: %s", baseMessage, ctx.Err().Error())
	}
	if ctx.Err() == context.Canceled {
		return "La fila fue cancelada", fmt.Sprintf("%s: %s", baseMessage, ctx.Err().Error())
	}
	return baseMessage, err.Error()
}

// classifyRowTimeoutWhileImages explica qué se llegó a hacer antes del corte
// cuando el producto ya estaba impactado pero las imágenes no terminaron.
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
