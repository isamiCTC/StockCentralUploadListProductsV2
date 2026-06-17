package results

import (
	"path/filepath"
	"testing"

	"stockcentraluploadlistproductsv2/internal/domain"
	"stockcentraluploadlistproductsv2/internal/excel"

	"github.com/xuri/excelize/v2"
)

func TestWriteRowResultsOmitsSkippedRowsAndWritesExpectedSheet(t *testing.T) {
	t.Parallel()

	root := t.TempDir()
	outputPath := filepath.Join(root, "results", "catalog.result.xlsx")

	writer := NewWriter()
	err := writer.WriteRowResults(outputPath, []domain.RowResult{
		{ExcelRowNumber: 2, SKU: "SKIP-1", Status: domain.RowStatusSkipped},
		{
			ExcelRowNumber: 3,
			SKU:            "ABC123",
			Status:         domain.RowStatusOK,
			ProductResult:  "ACTUALIZADO",
			ImagesResult:   "OK",
			Message:        "Todo bien",
			Detail:         "status=200",
		},
	})
	if err != nil {
		t.Fatalf("WriteRowResults returned error: %v", err)
	}

	file, err := excelize.OpenFile(outputPath)
	if err != nil {
		t.Fatalf("OpenFile returned error: %v", err)
	}
	defer func() { _ = file.Close() }()

	if got := file.GetSheetName(0); got != "Resultados" {
		t.Fatalf("sheet name = %q, want Resultados", got)
	}
	if got, _ := file.GetCellValue("Resultados", "B2"); got != "ABC123" {
		t.Fatalf("B2 = %q, want ABC123", got)
	}
	if got, _ := file.GetCellValue("Resultados", "A3"); got != "" {
		t.Fatalf("A3 = %q, want empty because skipped rows are omitted", got)
	}
}

func TestWriteStructureErrorsWritesExpectedRows(t *testing.T) {
	t.Parallel()

	root := t.TempDir()
	outputPath := filepath.Join(root, "results", "catalog.structure-errors.xlsx")

	writer := NewWriter()
	err := writer.WriteStructureErrors(outputPath, []excel.StructureError{
		{Field: "SKU", Message: "Falta columna", Detail: "No se encontró SKU"},
	})
	if err != nil {
		t.Fatalf("WriteStructureErrors returned error: %v", err)
	}

	file, err := excelize.OpenFile(outputPath)
	if err != nil {
		t.Fatalf("OpenFile returned error: %v", err)
	}
	defer func() { _ = file.Close() }()

	if got := file.GetSheetName(0); got != "ErroresEstructura" {
		t.Fatalf("sheet name = %q, want ErroresEstructura", got)
	}
	if got, _ := file.GetCellValue("ErroresEstructura", "A2"); got != "SKU" {
		t.Fatalf("A2 = %q, want SKU", got)
	}
	if got, _ := file.GetCellValue("ErroresEstructura", "B2"); got != "Falta columna" {
		t.Fatalf("B2 = %q, want Falta columna", got)
	}
}
