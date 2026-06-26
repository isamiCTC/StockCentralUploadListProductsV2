package reporting

import (
	"context"
	"fmt"
	"strings"

	"stockcentraluploadlistproductsv2/internal/catalog"
)

// Este archivo traduce hechos técnicos del procesamiento a textos finales
// orientados al Excel que ve negocio o cliente final.
//
// La idea es separar:
// - qué pasó realmente en producto, categoría e imágenes;
// - de cómo queremos contarlo en `Estado`, `Mensaje` y `Detalle`.
//
// Así evitamos mezclar redacción visible al usuario con la orquestación técnica
// que vive en `batch/file_processor.go`.

// ImageSyncFacts resume el resultado agregado de imágenes de una fila.
//
// No describe requests ni errores internos de bajo nivel. Solo conserva los
// conteos funcionales mínimos necesarios para redactar el resultado final.
type ImageSyncFacts struct {
	CreatedCount   int
	UpdatedCount   int
	UnchangedCount int
	FailedCount    int
}

// RowPresentation agrupa únicamente los campos "presentables" del resultado.
//
// `file_processor` sigue siendo dueño de otros datos de la fila como SKU,
// provider o la acción técnica traducida a `ProductResult`.
type RowPresentation struct {
	Status       RowStatus
	ImagesResult string
	Message      string
	Detail       string
}

// BuildStockSuccessPresentation devuelve el texto visible para una fila de
// actualización de stock que terminó correctamente.
func BuildStockSuccessPresentation() RowPresentation {
	return RowPresentation{
		Status:       RowStatusOK,
		ImagesResult: "NO_APLICA",
		Message:      "Stock actualizado correctamente",
		Detail:       "El stock del producto fue actualizado correctamente.",
	}
}

// BuildFullImportPresentation arma el resultado visible de una fila de importación completa.
//
// Recibe hechos ya resueltos por el procesamiento:
// - si el producto se creó o actualizó;
// - si la categoría cayó en la rama general;
// - si se procesaron imágenes;
// - cuántas se crearon, actualizaron, quedaron iguales o fallaron;
// - y si hubo una interrupción por timeout/cancelación durante imágenes.
func BuildFullImportPresentation(productAction string, categorySource catalog.ResolutionSource, imageProcessingApplied bool, imageFacts ImageSyncFacts, interruptedErr error) RowPresentation {
	hasCategoryObservation := categorySource == catalog.ResolutionSourceFallback
	hasImageObservation := imageFacts.FailedCount > 0 || interruptedErr != nil
	hasObservations := hasCategoryObservation || hasImageObservation

	status := RowStatusOK
	if hasObservations {
		status = RowStatusPartialOK
	}

	imagesResult := "NO_APLICA"
	if imageProcessingApplied {
		imagesResult = "OK"
		if hasImageObservation {
			imagesResult = "PARCIAL"
		}
	}

	details := make([]string, 0, 6)
	if hasCategoryObservation {
		details = append(details, "La categoría informada no pudo identificarse y se asignó una categoría general al producto.")
	}

	if !imageProcessingApplied {
		details = append(details, "No se procesaron imágenes para este producto.")
	} else {
		details = append(details, buildImageDetailSentences(imageFacts)...)
		if interruptedErr != nil {
			details = append(details, interruptionSentence(interruptedErr))
		}
	}

	return RowPresentation{
		Status:       status,
		ImagesResult: imagesResult,
		Message:      buildProductMessage(productAction, hasObservations),
		Detail:       strings.Join(details, " "),
	}
}

func buildProductMessage(productAction string, hasObservations bool) string {
	switch strings.ToUpper(strings.TrimSpace(productAction)) {
	case "CREATE":
		if hasObservations {
			return "Producto creado con observaciones"
		}
		return "Producto creado correctamente"
	default:
		if hasObservations {
			return "Producto actualizado con observaciones"
		}
		return "Producto actualizado correctamente"
	}
}

func buildImageDetailSentences(facts ImageSyncFacts) []string {
	details := make([]string, 0, 4)

	if facts.CreatedCount > 0 {
		details = append(details, countSentence(facts.CreatedCount, "Se agregó %d imagen nueva.", "Se agregaron %d imágenes nuevas."))
	}
	if facts.UpdatedCount > 0 {
		details = append(details, countSentence(facts.UpdatedCount, "Se actualizó %d imagen correctamente.", "Se actualizaron %d imágenes correctamente."))
	}
	if facts.UnchangedCount > 0 {
		details = append(details, countSentence(facts.UnchangedCount, "%d imagen ya se encontraba cargada.", "%d imágenes ya se encontraban cargadas."))
	}
	if facts.FailedCount > 0 {
		details = append(details, countSentence(facts.FailedCount, "%d imagen no pudo procesarse.", "%d imágenes no pudieron procesarse."))
	}

	if len(details) == 0 {
		details = append(details, "No se registraron cambios en las imágenes del producto.")
	}

	return details
}

func interruptionSentence(err error) string {
	switch err {
	case context.DeadlineExceeded:
		return "El procesamiento de imágenes no pudo completarse dentro del tiempo esperado."
	case context.Canceled:
		return "El procesamiento de imágenes fue interrumpido antes de completarse."
	default:
		return "El procesamiento de imágenes no pudo completarse."
	}
}

func countSentence(count int, singularTemplate, pluralTemplate string) string {
	if count == 1 {
		return fmt.Sprintf(singularTemplate, count)
	}
	return fmt.Sprintf(pluralTemplate, count)
}
