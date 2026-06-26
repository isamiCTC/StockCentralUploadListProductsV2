package reporting

import (
	"context"
	"testing"

	"stockcentraluploadlistproductsv2/internal/catalog"
)

// Este archivo prueba la traducción desde hechos técnicos a mensajes legibles
// dentro del Excel final de resultados.
//
// El foco está en blindar textos y estados de negocio sin depender del
// procesamiento completo del batch.

func TestBuildStockSuccessPresentation(t *testing.T) {
	t.Parallel()

	got := BuildStockSuccessPresentation()

	if got.Status != RowStatusOK {
		t.Fatalf("Status = %q, want %q", got.Status, RowStatusOK)
	}
	if got.Message != "Stock actualizado correctamente" {
		t.Fatalf("Message = %q", got.Message)
	}
	if got.Detail != "El stock del producto fue actualizado correctamente." {
		t.Fatalf("Detail = %q", got.Detail)
	}
}

func TestBuildFullImportPresentationReturnsOKForUpdatedProductWithUnchangedImages(t *testing.T) {
	t.Parallel()

	got := BuildFullImportPresentation("UPDATE", catalog.ResolutionSourceDatabase, true, ImageSyncFacts{
		UnchangedCount: 2,
	}, nil)

	if got.Status != RowStatusOK {
		t.Fatalf("Status = %q, want %q", got.Status, RowStatusOK)
	}
	if got.ImagesResult != "OK" {
		t.Fatalf("ImagesResult = %q, want %q", got.ImagesResult, "OK")
	}
	if got.Message != "Producto actualizado correctamente" {
		t.Fatalf("Message = %q", got.Message)
	}
	if got.Detail != "2 imágenes ya se encontraban cargadas." {
		t.Fatalf("Detail = %q", got.Detail)
	}
}

func TestBuildFullImportPresentationReturnsPartialForFallbackCategory(t *testing.T) {
	t.Parallel()

	got := BuildFullImportPresentation("CREATE", catalog.ResolutionSourceFallback, false, ImageSyncFacts{}, nil)

	if got.Status != RowStatusPartialOK {
		t.Fatalf("Status = %q, want %q", got.Status, RowStatusPartialOK)
	}
	if got.Message != "Producto creado con observaciones" {
		t.Fatalf("Message = %q", got.Message)
	}
	want := "La categoría informada no pudo identificarse y se asignó una categoría general al producto. No se procesaron imágenes para este producto."
	if got.Detail != want {
		t.Fatalf("Detail = %q, want %q", got.Detail, want)
	}
}

func TestBuildFullImportPresentationReturnsPartialForInterruptedImages(t *testing.T) {
	t.Parallel()

	got := BuildFullImportPresentation("UPDATE", catalog.ResolutionSourceAPI, true, ImageSyncFacts{
		UpdatedCount: 1,
	}, context.DeadlineExceeded)

	if got.Status != RowStatusPartialOK {
		t.Fatalf("Status = %q, want %q", got.Status, RowStatusPartialOK)
	}
	if got.ImagesResult != "PARCIAL" {
		t.Fatalf("ImagesResult = %q, want %q", got.ImagesResult, "PARCIAL")
	}
	want := "Se actualizó 1 imagen correctamente. El procesamiento de imágenes no pudo completarse dentro del tiempo esperado."
	if got.Detail != want {
		t.Fatalf("Detail = %q, want %q", got.Detail, want)
	}
}
