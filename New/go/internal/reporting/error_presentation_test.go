package reporting

import (
	"context"
	"errors"
	"testing"
)

// Este archivo prueba la traducción de errores técnicos a textos visibles
// para el Excel de resultados.

func TestBuildErrorPresentationHumanizesHTTPAPIError(t *testing.T) {
	t.Parallel()

	err := errors.New(`update product ABC123 failed with status 400 body="{\"Message\":\"validation failed\"}"`)

	message, detail := BuildErrorPresentation(context.Background(), "Falló el alta o actualización del producto", err)

	if message != "Falló el alta o actualización del producto" {
		t.Fatalf("message = %q", message)
	}
	wantDetail := "La API rechazó la operación: validation failed."
	if detail != wantDetail {
		t.Fatalf("detail = %q, want %q", detail, wantDetail)
	}
}

func TestBuildErrorPresentationHumanizesImageDownloadError(t *testing.T) {
	t.Parallel()

	err := errors.New("download image \"https://example.test/a.jpg\" returned status 404")

	_, detail := BuildErrorPresentation(context.Background(), "No se pudieron procesar las imágenes", err)

	wantDetail := "No se pudo descargar una imagen porque la URL informada no existe."
	if detail != wantDetail {
		t.Fatalf("detail = %q, want %q", detail, wantDetail)
	}
}
