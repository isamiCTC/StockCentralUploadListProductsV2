package excel

import "testing"

// Este archivo valida las reglas estructurales del workbook antes de mapear filas.

func TestValidateStructureAcceptsLaxHeaders(t *testing.T) {
	t.Parallel()

	headers := []HeaderCell{
		{Index: 0, Original: "sku", Normalized: NormalizeHeader("sku")},
		{Index: 1, Original: " stock ", Normalized: NormalizeHeader(" stock ")},
	}

	indexByKey, errors := ValidateStructure(FileFormatStockUpdate, headers)
	if len(errors) != 0 {
		t.Fatalf("ValidateStructure returned unexpected errors: %+v", errors)
	}
	if indexByKey[NormalizeHeader("SKU")] != 0 {
		t.Fatalf("SKU index = %d, want 0", indexByKey[NormalizeHeader("SKU")])
	}
	if indexByKey[NormalizeHeader("STOCK")] != 1 {
		t.Fatalf("STOCK index = %d, want 1", indexByKey[NormalizeHeader("STOCK")])
	}
}

func TestValidateStructureReportsMissingColumns(t *testing.T) {
	t.Parallel()

	headers := []HeaderCell{
		{Index: 0, Original: "sku", Normalized: NormalizeHeader("sku")},
	}

	_, errors := ValidateStructure(FileFormatStockUpdate, headers)
	if len(errors) != 1 {
		t.Fatalf("ValidateStructure errors count = %d, want 1", len(errors))
	}
	if errors[0].Field != "STOCK" {
		t.Fatalf("missing field = %q, want STOCK", errors[0].Field)
	}
}

func TestValidateStructureReportsDuplicates(t *testing.T) {
	t.Parallel()

	headers := []HeaderCell{
		{Index: 0, Original: "SKU", Normalized: NormalizeHeader("SKU")},
		{Index: 1, Original: "STOCK", Normalized: NormalizeHeader("STOCK")},
		{Index: 2, Original: "Stock", Normalized: NormalizeHeader("Stock")},
	}

	_, errors := ValidateStructure(FileFormatStockUpdate, headers)
	if len(errors) != 1 {
		t.Fatalf("ValidateStructure errors count = %d, want 1", len(errors))
	}
	if errors[0].Message != "Columna duplicada" {
		t.Fatalf("duplicate message = %q, want %q", errors[0].Message, "Columna duplicada")
	}
}
