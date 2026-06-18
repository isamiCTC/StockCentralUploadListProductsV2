package workbook

import "testing"

// Este archivo prueba la normalización laxa de headers y celdas.

func TestNormalizeHeader(t *testing.T) {
	t.Parallel()

	got := NormalizeHeader("  Sub   Categoría ")
	want := "sub categoria"
	if got != want {
		t.Fatalf("NormalizeHeader = %q, want %q", got, want)
	}
}

func TestNormalizeCell(t *testing.T) {
	t.Parallel()

	got := NormalizeCell("  valor con espacios  ")
	if got != "valor con espacios" {
		t.Fatalf("NormalizeCell = %q, want %q", got, "valor con espacios")
	}
}
