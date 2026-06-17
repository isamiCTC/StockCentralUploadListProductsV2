package excel

import "testing"

// Este archivo prueba el mapping principal del Excel a DTOs tipados.

func TestMapRowsFullImportHappyPath(t *testing.T) {
	t.Parallel()

	workbook := Workbook{
		Format: FileFormatFullImport,
		HeaderIndexByKey: map[string]int{
			NormalizeHeader("SKU"):             0,
			NormalizeHeader("NOMBRE"):          1,
			NormalizeHeader("MARCA"):           2,
			NormalizeHeader("DESCRIPCION"):     3,
			NormalizeHeader("ALTO"):            4,
			NormalizeHeader("ANCHO"):           5,
			NormalizeHeader("LARGO"):           6,
			NormalizeHeader("PESO"):            7,
			NormalizeHeader("URL IMAGENES"):    8,
			NormalizeHeader("PRECIO"):          9,
			NormalizeHeader("IVA"):             10,
			NormalizeHeader("TIPO"):            11,
			NormalizeHeader("AHORA"):           12,
			NormalizeHeader("CATEGORIA"):       13,
			NormalizeHeader("SUB CATEGORIA"):   14,
			NormalizeHeader("STOCK"):           15,
			NormalizeHeader("OFERTA"):          16,
			NormalizeHeader("FECHA DE INICIO"): 17,
			NormalizeHeader("FECHA DE FIN"):    18,
		},
		Rows: []RawRow{
			{
				ExcelRowNumber: 2,
				Values: []string{
					"ABC123", "Producto", "Marca", "Descripcion", "10", "20", "30",
					"1500", " https://img1.test/a.jpg & https://img2.test/b.jpg ",
					"1000", "0,21", "TIPO", "AHORA", "Cat", "Audio", "5", "900",
					"2026-01-01", "2026-01-31",
				},
			},
		},
	}

	rows, err := MapRows(workbook)
	if err != nil {
		t.Fatalf("MapRows returned error: %v", err)
	}
	if len(rows) != 1 {
		t.Fatalf("rows count = %d, want 1", len(rows))
	}
	if rows[0].HasErrors() {
		t.Fatalf("mapped row should not have errors: %+v", rows[0].Issues)
	}
	if rows[0].FullImport == nil {
		t.Fatal("FullImport payload should not be nil")
	}
	if rows[0].FullImport.WeightKilograms != 1.5 {
		t.Fatalf("WeightKilograms = %v, want 1.5", rows[0].FullImport.WeightKilograms)
	}
	if rows[0].FullImport.Price != 900 {
		t.Fatalf("Price = %v, want 900 because offer should overwrite price", rows[0].FullImport.Price)
	}
	if rows[0].FullImport.Taxes != 21 {
		t.Fatalf("Taxes = %v, want 21", rows[0].FullImport.Taxes)
	}
	if !rows[0].FullImport.SyncImages {
		t.Fatal("SyncImages should be true when valid image URLs are present")
	}
	if len(rows[0].FullImport.ImageURLs) != 2 {
		t.Fatalf("ImageURLs count = %d, want 2", len(rows[0].FullImport.ImageURLs))
	}
}

func TestMapRowsFullImportInvalidImageURLProducesError(t *testing.T) {
	t.Parallel()

	workbook := Workbook{
		Format: FileFormatFullImport,
		HeaderIndexByKey: map[string]int{
			NormalizeHeader("SKU"):           0,
			NormalizeHeader("NOMBRE"):        1,
			NormalizeHeader("MARCA"):         2,
			NormalizeHeader("DESCRIPCION"):   3,
			NormalizeHeader("ALTO"):          4,
			NormalizeHeader("ANCHO"):         5,
			NormalizeHeader("LARGO"):         6,
			NormalizeHeader("PESO"):          7,
			NormalizeHeader("URL IMAGENES"):  8,
			NormalizeHeader("PRECIO"):        9,
			NormalizeHeader("IVA"):           10,
			NormalizeHeader("TIPO"):          11,
			NormalizeHeader("AHORA"):         12,
			NormalizeHeader("CATEGORIA"):     13,
			NormalizeHeader("SUB CATEGORIA"): 14,
			NormalizeHeader("STOCK"):         15,
			NormalizeHeader("OFERTA"):        16,
		},
		Rows: []RawRow{
			{
				ExcelRowNumber: 2,
				Values: []string{
					"ABC123", "Producto", "Marca", "Descripcion", "10", "20", "30",
					"1500", "notaurl", "1000", "21", "TIPO", "AHORA", "Cat", "Audio", "5", "",
				},
			},
		},
	}

	rows, err := MapRows(workbook)
	if err != nil {
		t.Fatalf("MapRows returned error: %v", err)
	}
	if len(rows) != 1 {
		t.Fatalf("rows count = %d, want 1", len(rows))
	}
	if !rows[0].HasErrors() {
		t.Fatal("mapped row should have an error for invalid image URL")
	}
}
