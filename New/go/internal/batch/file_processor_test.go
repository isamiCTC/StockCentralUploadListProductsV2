package batch

import (
	"context"
	"io"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"stockcentraluploadlistproductsv2/internal/catalog"
	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/images"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/products"
	"stockcentraluploadlistproductsv2/internal/reporting"
	"stockcentraluploadlistproductsv2/internal/workbook"
)

// Este archivo prueba el flujo de procesamiento de un archivo individual.
//
// La suite cubre tanto defaults y helpers como ramas funcionales del
// `FileProcessor`, incluyendo filas, productos, imágenes y resultados finales.

func TestNewFileProcessorAppliesDefaults(t *testing.T) {
	t.Parallel()

	processor := NewFileProcessor(0, 0, false, nil, nil, nil, nil, nil, nil, nil, discardLoggerSet())

	if processor.rowWorkers != 1 {
		t.Fatalf("rowWorkers = %d, want 1", processor.rowWorkers)
	}
	if processor.rowTimeout != 120*time.Second {
		t.Fatalf("rowTimeout = %s, want %s", processor.rowTimeout, 120*time.Second)
	}
}

func TestSummarizeRowResultsIgnoresSkippedAndCountsFinalStates(t *testing.T) {
	t.Parallel()

	processed, successful, partial, errors := summarizeRowResults([]reporting.RowResult{
		{Status: reporting.RowStatusSkipped},
		{Status: reporting.RowStatusOK},
		{Status: reporting.RowStatusPartialOK},
		{Status: reporting.RowStatusError},
	})

	if processed != 3 || successful != 1 || partial != 1 || errors != 1 {
		t.Fatalf("summarizeRowResults = (%d, %d, %d, %d), want (3, 1, 1, 1)", processed, successful, partial, errors)
	}
}

func TestResolveFileStatusReturnsProcessedWithErrorsWhenAnyRowFailedOrPartial(t *testing.T) {
	t.Parallel()

	if got := resolveFileStatus(0, 0); got != reporting.FileStatusProcessed {
		t.Fatalf("resolveFileStatus(0, 0) = %q, want %q", got, reporting.FileStatusProcessed)
	}
	if got := resolveFileStatus(1, 0); got != reporting.FileStatusProcessedErrors {
		t.Fatalf("resolveFileStatus(1, 0) = %q, want %q", got, reporting.FileStatusProcessedErrors)
	}
	if got := resolveFileStatus(0, 1); got != reporting.FileStatusProcessedErrors {
		t.Fatalf("resolveFileStatus(0, 1) = %q, want %q", got, reporting.FileStatusProcessedErrors)
	}
}

func TestProcessMappedRowsMarksStockRowAsErrorWhenRowTimeoutExpires(t *testing.T) {
	t.Parallel()

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(150 * time.Millisecond)
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"Result":{"Sku":"ABC123","Stock":5}}`))
	}))
	defer server.Close()

	client := products.NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        server.URL,
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")

	processor := &FileProcessor{
		rowWorkers:     1,
		rowTimeout:     25 * time.Millisecond,
		productsClient: client,
		logs:           discardLoggerSet(),
	}

	rows := []workbook.MappedRow{
		{
			ExcelRowNumber: 2,
			SKU:            "ABC123",
			StockUpdate:    &workbook.StockUpdateRow{SKU: "ABC123", Stock: 10},
		},
	}

	results, err := processor.processMappedRows(context.Background(), 342, workbook.FileFormatStockUpdate, rows)
	if err != nil {
		t.Fatalf("processMappedRows returned error: %v", err)
	}
	if len(results) != 1 {
		t.Fatalf("len(results) = %d, want 1", len(results))
	}
	if results[0].Status != reporting.RowStatusError {
		t.Fatalf("Status = %q, want %q", results[0].Status, reporting.RowStatusError)
	}
	if results[0].Message != "La fila excedió el timeout configurado" {
		t.Fatalf("Message = %q, want timeout message", results[0].Message)
	}
}

func TestProcessFullImportRowMarksPartialWhenTimeoutExpiresDuringImages(t *testing.T) {
	t.Parallel()

	apiServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		switch r.Method {
		case http.MethodPut:
			_, _ = w.Write([]byte(`{}`))
		default:
			http.Error(w, "unexpected method", http.StatusMethodNotAllowed)
		}
	}))
	defer apiServer.Close()

	imageServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(2 * time.Second)
		w.Header().Set("Content-Type", "image/jpeg")
		_, _ = w.Write([]byte("not-reached-before-timeout"))
	}))
	defer imageServer.Close()

	client := products.NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        apiServer.URL,
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")

	processor := &FileProcessor{
		rowWorkers: 1,
		rowTimeout: 250 * time.Millisecond,
		syncImages: true,
		catalogResolver: catalog.NewResolver(client, map[string]products.CategoryBranch{
			"CELULARES": {Code: "1217", Name: "Telefonía y Accesorios"},
		}, products.CategoryBranch{Code: "1041", Name: "Varios"}),
		imageDownloader: images.NewDownloader(5 * time.Second),
		productsClient:  client,
		logs:            discardLoggerSet(),
	}

	row := workbook.MappedRow{
		ExcelRowNumber: 3,
		SKU:            "XYZ999",
		FullImport: &workbook.FullImportRow{
			SKU:              "XYZ999",
			Name:             "Producto test",
			Brand:            "Marca",
			Description:      "Descripcion",
			ShortDescription: "Corta",
			Stock:            10,
			Price:            100,
			ListPrice:        120,
			NetPrice:         90,
			Taxes:            21,
			Height:           1,
			Width:            2,
			Depth:            3,
			WeightKilograms:  0.5,
			SubCategory:      "CELULARES",
			SyncImages:       true,
			ImageURLs:        []string{imageServer.URL + "/image.jpg"},
		},
	}

	rowCtx, cancel := context.WithTimeout(context.Background(), 250*time.Millisecond)
	defer cancel()

	result := processor.processFullImportRow(rowCtx, 342, row, discardLoggerSet().Detail.NewBuffer())

	if result.Status != reporting.RowStatusPartialOK {
		t.Fatalf("Status = %q, want %q", result.Status, reporting.RowStatusPartialOK)
	}
	if result.ProductResult != "ACTUALIZADO" {
		t.Fatalf("ProductResult = %q, want %q", result.ProductResult, "ACTUALIZADO")
	}
	if result.ImagesResult != "PARCIAL" {
		t.Fatalf("ImagesResult = %q, want %q", result.ImagesResult, "PARCIAL")
	}
	if result.Message != "Producto impactado pero la fila excedió el timeout durante imágenes" {
		t.Fatalf("Message = %q, want timeout during images message", result.Message)
	}
}

func discardLoggerSet() logging.LoggerSet {
	return logging.LoggerSet{
		Summary: logging.New(logging.LevelDebug, io.Discard),
		Detail:  logging.New(logging.LevelDebug, io.Discard),
	}
}
