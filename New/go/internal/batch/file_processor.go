package batch

import (
	"context"
	"encoding/json"
	"fmt"
	"slices"
	"strings"
	"sync"
	"time"

	"stockcentraluploadlistproductsv2/internal/catalog"
	"stockcentraluploadlistproductsv2/internal/images"
	"stockcentraluploadlistproductsv2/internal/intake"
	productsapi "stockcentraluploadlistproductsv2/internal/integrations/productsapi"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/notifications"
	"stockcentraluploadlistproductsv2/internal/reporting"
	"stockcentraluploadlistproductsv2/internal/results"
	"stockcentraluploadlistproductsv2/internal/workbook"
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
//
// Es, en la práctica, el corazón más largo del proceso.
// Por eso este archivo está comentado de forma bastante guiada: la idea es que
// alguien pueda seguirlo casi como si fuera un paso a paso narrado.
type FileProcessor struct {
	rowWorkers      int
	rowTimeout      time.Duration
	syncImages      bool
	catalogResolver *catalog.Resolver
	excelReader     *workbook.Reader
	imageDownloader *images.Downloader
	productsClient  *productsapi.Client
	mover           *intake.Mover
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
	excelReader *workbook.Reader,
	imageDownloader *images.Downloader,
	productsClient *productsapi.Client,
	mover *intake.Mover,
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
func (p *FileProcessor) Process(ctx context.Context, job intake.FileJob) (reporting.FileResult, error) {
	// Paso 0. Guardar cuándo empezó este archivo.
	// Lo usamos después para métricas y auditoría.
	startedAt := time.Now()

	// Paso 1. A partir del job base armamos todas las rutas derivadas:
	// processing, processed y archivos auxiliares de salida.
	job = p.mover.BuildPaths(job)

	// Summary deja una traza corta del archivo que arranca.
	p.logs.Summary.Info("file-start",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.RelativePath),
	)

	// Detail deja la ruta concreta para depuración más fina.
	p.logs.Detail.Blank()
	p.logs.Detail.Info("--------------------------------------------------- FILE START ---------------------------------------------------")
	p.logs.Detail.Info("file-processing-started",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.InputPath),
	)

	// Paso 2. Antes de leerlo, movemos el archivo a `processing` para separarlo
	// claramente de los archivos todavía pendientes.
	job, err := p.mover.MoveToProcessing(job)
	if err != nil {
		return reporting.FileResult{
			ProviderID:    job.ProviderID,
			InputPath:     job.InputPath,
			Status:        reporting.FileStatusFailed,
			StartedAt:     startedAt,
			FinishedAt:    time.Now(),
			FailureReason: err.Error(),
		}, err
	}

	p.logs.Detail.Info("file-moved-to-processing",
		logging.Int("provider_id", job.ProviderID),
		logging.String("processing_path", job.ProcessingPath),
	)

	// Paso 3. Leer el Excel.
	// El reader ya devuelve metadata útil y validaciones estructurales básicas.
	workbookData, err := p.excelReader.Read(job.ProcessingPath)
	if err != nil {
		return reporting.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         reporting.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	// Paso 4. Si el layout del Excel no sirve, entramos en un flujo especial que
	// escribe `ErroresEstructura` en lugar de resultados por fila.
	if !workbookData.IsStructureValid() {
		return p.finishWithStructureErrors(ctx, startedAt, job, workbookData)
	}

	// Paso 5. A esta altura el archivo ya tiene estructura válida y se puede mapear.
	p.logs.Detail.Info("excel-validated",
		logging.Int("provider_id", job.ProviderID),
		logging.String("sheet_name", workbookData.SheetName),
		logging.String("format", string(workbookData.Format)),
		logging.Int("rows_detected", countNonEmptyRows(workbookData.Rows)),
	)

	// Paso 6. Convertir filas crudas en filas tipadas que el negocio pueda usar.
	mappedRows, err := workbook.MapRows(workbookData)
	if err != nil {
		return reporting.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         reporting.FileStatusFailed,
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

	// Paso 7. Procesar las filas reales del archivo.
	// Acá recién empiezan llamadas a API, categorías, imágenes, etc.
	rowResults, err := p.processMappedRows(ctx, job.ProviderID, workbookData.Format, mappedRows)
	if err != nil {
		return reporting.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         reporting.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	// Paso 8. Recién cuando ya tenemos todos los resultados por fila movemos el
	// original a `processed`.
	job, err = p.mover.MoveToProcessed(job)
	if err != nil {
		return reporting.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         reporting.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  err.Error(),
		}, err
	}

	// Paso 9. Escribir el Excel final de resultados.
	// Este archivo es lo que normalmente va a mirar negocio.
	if err := p.resultsWriter.WriteRowResults(job.ResultsPath, rowResults); err != nil {
		return reporting.FileResult{
			ProviderID:      job.ProviderID,
			InputPath:       job.InputPath,
			ProcessingPath:  job.ProcessingPath,
			ProcessedPath:   job.ProcessedPath,
			Status:          reporting.FileStatusFailed,
			StartedAt:       startedAt,
			FinishedAt:      time.Now(),
			FailureReason:   err.Error(),
			ResultsFilePath: job.ResultsPath,
		}, fmt.Errorf("write row results workbook: %w", err)
	}

	// Paso 10. Consolidar métricas finales del archivo a partir de las filas.
	processedRows, successfulRows, partialRows, errorRows := summarizeRowResults(rowResults)
	result := reporting.FileResult{
		ProviderID:          job.ProviderID,
		InputPath:           job.InputPath,
		ProcessingPath:      job.ProcessingPath,
		ProcessedPath:       job.ProcessedPath,
		Status:              resolveFileStatus(errorRows, partialRows),
		DetectedRows:        countNonEmptyRows(workbookData.Rows),
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
	p.logs.Detail.Info("---------------------------------------------------- FILE END ----------------------------------------------------")
	p.logs.Detail.Blank()

	// Paso 11. Intentar notificación al final.
	// Si el mail falla, lo registramos, pero no "deshacemos" el trabajo del archivo.
	p.notifyFileProcessed(ctx, job, result)

	return result, nil
}

// finishWithStructureErrors cierra el flujo especial cuando el layout del
// Excel es inválido y se debe generar `ErroresEstructura`.
func (p *FileProcessor) finishWithStructureErrors(ctx context.Context, startedAt time.Time, job intake.FileJob, workbookData workbook.Workbook) (reporting.FileResult, error) {
	// Este camino es más corto que el flujo normal porque no procesa filas:
	// el archivo queda rechazado por estructura antes de llegar al negocio.

	// Registramos uno por uno los problemas estructurales encontrados.
	for _, structureError := range workbookData.StructureErrors {
		p.logs.Summary.Warn("excel-structure-error",
			logging.Int("provider_id", job.ProviderID),
			logging.String("field", structureError.Field),
			logging.String("message", structureError.Message),
		)
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
		return reporting.FileResult{
			ProviderID:     job.ProviderID,
			InputPath:      job.InputPath,
			ProcessingPath: job.ProcessingPath,
			Status:         reporting.FileStatusFailed,
			StartedAt:      startedAt,
			FinishedAt:     time.Now(),
			FailureReason:  moveErr.Error(),
		}, moveErr
	}

	// En este escenario generamos una planilla de estructura en vez de una
	// planilla de resultados por fila.
	if err := p.resultsWriter.WriteStructureErrors(job.StructureErrPath, workbookData.StructureErrors); err != nil {
		return reporting.FileResult{
			ProviderID:          job.ProviderID,
			InputPath:           job.InputPath,
			ProcessingPath:      job.ProcessingPath,
			ProcessedPath:       job.ProcessedPath,
			Status:              reporting.FileStatusFailed,
			StartedAt:           startedAt,
			FinishedAt:          time.Now(),
			FailureReason:       err.Error(),
			StructureErrorsPath: job.StructureErrPath,
		}, fmt.Errorf("write structure errors workbook: %w", err)
	}

	result := reporting.FileResult{
		ProviderID:          job.ProviderID,
		InputPath:           job.InputPath,
		ProcessingPath:      job.ProcessingPath,
		ProcessedPath:       job.ProcessedPath,
		Status:              reporting.FileStatusStructureError,
		DetectedRows:        countNonEmptyRows(workbookData.Rows),
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
	p.logs.Detail.Info("---------------------------------------------------- FILE END ----------------------------------------------------")
	p.logs.Detail.Blank()

	p.notifyFileProcessed(ctx, job, result)

	return result, nil
}

// notifyFileProcessed dispara la notificación sin afectar el resultado del
// archivo si SendGrid falla.
func (p *FileProcessor) notifyFileProcessed(ctx context.Context, job intake.FileJob, result reporting.FileResult) {
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
func countNonEmptyRows(rows []workbook.RawRow) int {
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
func summarizeMappedRows(rows []workbook.MappedRow) (skippedRows, validRows, errorRows int) {
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
func summarizeRowResults(rows []reporting.RowResult) (processedRows, successfulRows, partialRows, errorRows int) {
	for _, row := range rows {
		switch row.Status {
		case reporting.RowStatusSkipped:
			continue
		case reporting.RowStatusError:
			processedRows++
			errorRows++
		case reporting.RowStatusPartialOK:
			processedRows++
			partialRows++
		case reporting.RowStatusOK:
			processedRows++
			successfulRows++
		}
	}

	return processedRows, successfulRows, partialRows, errorRows
}

// resolveFileStatus decide el estado final del archivo.
func resolveFileStatus(errorRows, partialRows int) reporting.FileStatus {
	if errorRows > 0 || partialRows > 0 {
		return reporting.FileStatusProcessedErrors
	}

	return reporting.FileStatusProcessed
}

// processMappedRows procesa filas con un worker pool fijo y devuelve los
// resultados ordenados por número de fila para escribir luego el Excel.
func (p *FileProcessor) processMappedRows(ctx context.Context, providerID int, format workbook.FileFormat, rows []workbook.MappedRow) ([]reporting.RowResult, error) {
	// jobs reparte trabajo a los workers; resultsChan junta lo que devuelve
	// cada fila ya procesada.
	jobs := make(chan workbook.MappedRow)
	resultsChan := make(chan reporting.RowResult, len(rows))

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
		// Este goroutine solo empuja filas al canal de trabajo.
		// Separarlo así deja más claro el productor de jobs.
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

	resultsData := make([]reporting.RowResult, 0, len(rows))
	for result := range resultsChan {
		// Los resultados llegan en orden no determinístico por la concurrencia.
		resultsData = append(resultsData, result)
	}

	if err := ctx.Err(); err != nil {
		return nil, err
	}

	// Reordenamos por número de fila para que el Excel final sea legible.
	// Sin esto, el orden del worker pool haría más difícil comparar con el Excel original.
	slices.SortFunc(resultsData, func(a, b reporting.RowResult) int {
		switch {
		case a.ExcelRowNumber < b.ExcelRowNumber:
			return -1
		case a.ExcelRowNumber > b.ExcelRowNumber:
			return 1
		default:
			return 0
		}
	})

	return resultsData, nil
}

// processSingleRow trata una fila como una transacción lógica completa.
func (p *FileProcessor) processSingleRow(ctx context.Context, providerID int, format workbook.FileFormat, row workbook.MappedRow) reporting.RowResult {
	// Paso 1. Las filas vacías no son error: simplemente no hacen nada.
	if row.IsEmpty {
		return reporting.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         reporting.RowStatusSkipped,
			Message:        "Fila vacía",
			Detail:         "La fila no contiene valores útiles y se omite.",
		}
	}

	// A partir de acá armamos un buffer exclusivo para esta fila.
	// Todo lo que se loguee dentro de este SKU se acumula y se escribe junto
	// al final, evitando que se mezcle con otras filas concurrentes.
	rowLogs := p.logs.Detail.NewBuffer()
	defer func() {
		rowLogs.Info(fmt.Sprintf("-------- FIN SKU: %s ----------", row.SKU),
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
		)
		rowLogs.Flush()
	}()

	// Paso 2. Si el mapper ya marcó problemas, no entramos a negocio.
	if row.HasErrors() {
		detail := joinRowIssues(row.Issues)
		rowLogs.Info(fmt.Sprintf("-------- SKU: %s ----------", row.SKU),
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
		)
		rowLogs.Error("row-mapping-error",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("detail", detail),
		)
		return reporting.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         reporting.RowStatusError,
			Message:        "La fila no pasó las validaciones previas",
			Detail:         detail,
		}
	}

	// Paso 3. A partir de acá la fila está lista para ejecutar lógica real.
	rowLogs.Info(fmt.Sprintf("-------- SKU: %s ----------", row.SKU),
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
	)

	// Paso 4. El formato define qué camino de negocio aplicar a esa fila.
	switch format {
	case workbook.FileFormatStockUpdate:
		return p.processStockUpdateRow(ctx, providerID, row, rowLogs)
	case workbook.FileFormatFullImport:
		return p.processFullImportRow(ctx, providerID, row, rowLogs)
	default:
		rowLogs.Error("row-unsupported-format",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("format", string(format)),
		)
		return reporting.RowResult{
			ProviderID:     providerID,
			ExcelRowNumber: row.ExcelRowNumber,
			SKU:            row.SKU,
			Status:         reporting.RowStatusError,
			Message:        "Formato de archivo no soportado",
			Detail:         fmt.Sprintf("Formato recibido: %s", format),
		}
	}
}

// processStockUpdateRow implementa el caso reducido de SKU + STOCK.
func (p *FileProcessor) processStockUpdateRow(ctx context.Context, providerID int, row workbook.MappedRow, rowLogs *logging.Buffer) reporting.RowResult {
	// Arrancamos con una base común y luego la vamos completando.
	result := baseRowResult(providerID, row)
	result.ProductResult = "NO_PROCESADO"
	result.ImagesResult = "NO_APLICA"
	result.Message = "Actualización de stock completada"

	rowLogs.Info("validation-stock-row-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
	)

	if row.StockUpdate == nil {
		result.Status = reporting.RowStatusError
		result.Message = "Fila sin payload de stock"
		result.Detail = "La fila pasó por el worker sin DTO de stock."
		return result
	}

	// En stock update la única operación de negocio es sincronizar stock.
	updateMeta, err := p.productsClient.SyncStockLegacy(ctx, providerID, row.StockUpdate.SKU, row.StockUpdate.Stock)
	if err != nil {
		result.Status = reporting.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "Falló la actualización de stock en la API", err)
		rowLogs.Error("stock-sync-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	// Si la API respondió bien, la fila queda totalmente OK.
	result.ProductResult = "ACTUALIZADO"
	applyPresentation(&result, reporting.BuildStockSuccessPresentation())

	rowLogs.Info("stock-sync-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("api", describeMeta(updateMeta)),
	)

	return result
}

// processFullImportRow implementa la importación completa de 19 columnas.
func (p *FileProcessor) processFullImportRow(ctx context.Context, providerID int, row workbook.MappedRow, rowLogs *logging.Buffer) reporting.RowResult {
	// Igual que en stock, partimos de un resultado base y lo enriquecemos.
	result := baseRowResult(providerID, row)
	result.ProductResult = "NO_PROCESADO"
	result.ImagesResult = "NO_APLICA"

	rowLogs.Info("validation-full-row-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
	)

	if row.FullImport == nil {
		result.Status = reporting.RowStatusError
		result.Message = "Fila sin payload completo"
		result.Detail = "La fila pasó por el worker sin DTO de importación."
		return result
	}

	// Primero resolvemos la rama de catálogo que necesita el producto.
	resolution, err := p.catalogResolver.ResolveBySubcategory(ctx, providerID, row.FullImport.SubCategory)
	if err != nil {
		result.Status = reporting.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "No se pudo resolver la subcategoría", err)
		rowLogs.Error("subcategory-resolution-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	rowLogs.Info("subcategory-resolution-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("source", string(resolution.Source)),
		logging.String("category_code", resolution.Branch.Code),
		logging.String("category_name", resolution.Branch.Name),
	)

	input := productsapi.ProductInput{
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
	rowLogs.Debug("product-payload-ready",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.Int("stock", row.FullImport.Stock),
		logging.String("price", fmt.Sprintf("%.2f", row.FullImport.Price)),
		logging.String("list_price", fmt.Sprintf("%.2f", row.FullImport.ListPrice)),
		logging.String("net_price", fmt.Sprintf("%.2f", row.FullImport.NetPrice)),
		logging.String("taxes", fmt.Sprintf("%.2f", row.FullImport.Taxes)),
		logging.Int("images_count", len(row.FullImport.ImageURLs)),
	)

	// Convertimos la fila al modelo que espera la API de productos.
	product := p.productsClient.BuildProductFromInput(providerID, input, resolution.Branch)
	rowLogs.Debug("product-request",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("json", marshalLogJSON(product)),
	)
	upsertResult, err := p.productsClient.UpsertProductLegacy(ctx, providerID, product)
	if err != nil {
		result.Status = reporting.RowStatusError
		result.Message, result.Detail = classifyRowError(ctx, "Falló el alta o actualización del producto", err)
		rowLogs.Error("product-upsert-failed",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.String("error", err.Error()),
		)
		return result
	}

	result.ProductResult = translateProductAction(upsertResult.Action)
	rowLogs.Info("product-upsert-ok",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("action", upsertResult.Action),
		logging.Int("update_attempts", upsertResult.UpdateAttempts),
		logging.Int("create_attempts", upsertResult.CreateAttempts),
	)
	rowLogs.Debug("product-response",
		logging.Int("provider_id", providerID),
		logging.Int("excel_row", row.ExcelRowNumber),
		logging.String("sku", row.SKU),
		logging.String("update_response", describeHTTPResponse(upsertResult.UpdateMeta)),
		logging.String("create_response", describeHTTPResponse(upsertResult.CreateMeta)),
	)

	// Si la sincronización global de imágenes está apagada, la fila termina acá.
	if !p.syncImages {
		applyPresentation(&result, reporting.BuildFullImportPresentation(upsertResult.Action, resolution.Source, false, reporting.ImageSyncFacts{}, nil))
		return result
	}

	// Si la fila no trajo imágenes válidas, también termina bien acá.
	if !row.FullImport.SyncImages {
		applyPresentation(&result, reporting.BuildFullImportPresentation(upsertResult.Action, resolution.Source, false, reporting.ImageSyncFacts{}, nil))
		return result
	}

	// Recién en este punto empezamos a tratar imágenes.
	imageFacts := p.syncRowImages(ctx, providerID, row, rowLogs)

	// Si el contexto venció durante imágenes, el producto ya pudo haber quedado
	// impactado, por eso devolvemos PARTIAL_OK.
	if ctx.Err() != nil {
		applyPresentation(&result, reporting.BuildFullImportPresentation(upsertResult.Action, resolution.Source, true, imageFacts, ctx.Err()))
		return result
	}

	// También queda parcial si alguna imagen falló aunque el producto se haya creado.
	if imageFacts.FailedCount > 0 {
		applyPresentation(&result, reporting.BuildFullImportPresentation(upsertResult.Action, resolution.Source, true, imageFacts, nil))
		return result
	}

	applyPresentation(&result, reporting.BuildFullImportPresentation(upsertResult.Action, resolution.Source, true, imageFacts, nil))
	return result
}

// syncRowImages procesa las imágenes de una fila y devuelve métricas útiles
// para el Excel final de resultados.
func (p *FileProcessor) syncRowImages(ctx context.Context, providerID int, row workbook.MappedRow, rowLogs *logging.Buffer) reporting.ImageSyncFacts {
	if row.FullImport == nil || len(row.FullImport.ImageURLs) == 0 {
		return reporting.ImageSyncFacts{}
	}

	facts := reporting.ImageSyncFacts{}
	for index, imageURL := range row.FullImport.ImageURLs {
		// Antes de iniciar cada imagen chequeamos si la fila ya venció.
		if err := ctx.Err(); err != nil {
			break
		}

		rowLogs.Info("image-sync-start",
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
				break
			}
			facts.FailedCount++
			rowLogs.Error("image-download-failed",
				logging.Int("provider_id", providerID),
				logging.Int("excel_row", row.ExcelRowNumber),
				logging.String("sku", row.SKU),
				logging.Int("image_index", index),
				logging.String("error", err.Error()),
			)
			continue
		}

		// Paso 2: enviar esa imagen a la API del producto.
		rowLogs.Debug("image-request",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.Int("image_index", index),
			logging.String("request", describeImageRequest(base64Image)),
		)
		syncResult, err := p.productsClient.SyncImageLegacy(ctx, providerID, row.SKU, index, base64Image)
		if err != nil {
			if ctx.Err() != nil {
				break
			}
			facts.FailedCount++
			rowLogs.Error("image-sync-failed",
				logging.Int("provider_id", providerID),
				logging.Int("excel_row", row.ExcelRowNumber),
				logging.String("sku", row.SKU),
				logging.Int("image_index", index),
				logging.String("error", err.Error()),
			)
			continue
		}

		// La API puede decir que la imagen ya era igual y que no hizo falta subirla.
		switch syncResult.Action {
		case "CREATE":
			facts.CreatedCount++
		case "SKIP_SAME_IMAGE":
			facts.UnchangedCount++
		default:
			facts.UpdatedCount++
		}
		rowLogs.Info("image-sync-ok",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.Int("image_index", index),
			logging.String("action", syncResult.Action),
		)
		rowLogs.Debug("image-response",
			logging.Int("provider_id", providerID),
			logging.Int("excel_row", row.ExcelRowNumber),
			logging.String("sku", row.SKU),
			logging.Int("image_index", index),
			logging.String("get_response", describeHTTPResponse(syncResult.GetMeta)),
			logging.String("update_response", describeHTTPResponse(syncResult.UpdateMeta)),
			logging.String("create_response", describeHTTPResponse(syncResult.CreateMeta)),
		)
	}

	return facts
}

// baseRowResult arma la estructura común para cualquier resultado de fila.
func baseRowResult(providerID int, row workbook.MappedRow) reporting.RowResult {
	return reporting.RowResult{
		ProviderID:     providerID,
		ExcelRowNumber: row.ExcelRowNumber,
		SKU:            row.SKU,
	}
}

// applyPresentation copia al `RowResult` los campos visibles al usuario final.
func applyPresentation(result *reporting.RowResult, presentation reporting.RowPresentation) {
	if result == nil {
		return
	}

	result.Status = presentation.Status
	result.ImagesResult = presentation.ImagesResult
	result.Message = presentation.Message
	result.Detail = presentation.Detail
}

// joinRowIssues convierte los problemas del mapper en un texto entendible.
func joinRowIssues(issues []workbook.RowIssue) string {
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

// describeHTTPResponse deja visible el status y una porción acotada del body.
func describeHTTPResponse(meta interface {
	GetStatusCode() int
	GetBody() []byte
}) string {
	if meta == nil {
		return ""
	}

	bodyText := strings.TrimSpace(string(meta.GetBody()))
	if bodyText == "" {
		return fmt.Sprintf("status=%d", meta.GetStatusCode())
	}

	const maxBodyLength = 500
	if len(bodyText) > maxBodyLength {
		bodyText = bodyText[:maxBodyLength] + "..."
	}

	return fmt.Sprintf("status=%d body=%q", meta.GetStatusCode(), bodyText)
}

// classifyRowError traduce un error técnico a un mensaje de fila más claro,
// respetando si el contexto terminó por timeout o cancelación.
func classifyRowError(ctx context.Context, baseMessage string, err error) (message, detail string) {
	return reporting.BuildErrorPresentation(ctx, baseMessage, err)
}

func marshalLogJSON(value any) string {
	data, err := json.Marshal(value)
	if err != nil {
		return fmt.Sprintf("marshal-error:%v", err)
	}
	return string(data)
}

func describeImageRequest(base64Image string) string {
	const previewLength = 48

	preview := base64Image
	if len(preview) > previewLength {
		preview = preview[:previewLength] + "..."
	}

	return fmt.Sprintf("base64_len=%d preview=%q", len(base64Image), preview)
}
