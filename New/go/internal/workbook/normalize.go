package workbook

import (
	"strings"
	"unicode"
)

// Este archivo encapsula la normalización laxa acordada para headers y strings
// operativos del Excel.
//
// Responsabilidad del archivo:
// - convertir valores "visualmente distintos" en claves comparables
// - evitar fragilidad innecesaria por mayúsculas, tildes o espacios
//
// Reglas aplicadas especialmente a headers:
// - trim
// - minúsculas
// - sin tildes
// - espacios múltiples colapsados

// NormalizeHeader convierte un header al formato interno de comparación.
//
// Ejemplos que deberían colapsar al mismo valor:
// - `SUB CATEGORIA`
// - `sub categoría`
// - ` Sub   Categoria `
func NormalizeHeader(value string) string {
	trimmed := strings.TrimSpace(value)
	lower := strings.ToLower(trimmed)
	withoutAccents := removeAccents(lower)
	return collapseWhitespace(withoutAccents)
}

// NormalizeCell hace una normalización conservadora sobre un valor de celda.
//
// A diferencia del header, acá no queremos transformar demasiado el contenido.
// Por ahora:
// - removemos espacios externos
// - preservamos el valor interno casi intacto
//
// Eso nos permite no deformar descripciones o textos comerciales.
func NormalizeCell(value string) string {
	return strings.TrimSpace(value)
}

// collapseWhitespace reemplaza cualquier secuencia de espacios/blancos
// por un único espacio normal.
func collapseWhitespace(value string) string {
	var b strings.Builder
	lastWasSpace := false

	for _, r := range value {
		if unicode.IsSpace(r) {
			if !lastWasSpace {
				b.WriteRune(' ')
				lastWasSpace = true
			}
			continue
		}

		b.WriteRune(r)
		lastWasSpace = false
	}

	return strings.TrimSpace(b.String())
}

// removeAccents convierte vocales acentuadas y la ñ a una forma básica.
// Es una solución simple y explícita, suficiente para headers en español.
func removeAccents(value string) string {
	replacer := strings.NewReplacer(
		"á", "a",
		"à", "a",
		"ä", "a",
		"â", "a",
		"ã", "a",
		"é", "e",
		"è", "e",
		"ë", "e",
		"ê", "e",
		"í", "i",
		"ì", "i",
		"ï", "i",
		"î", "i",
		"ó", "o",
		"ò", "o",
		"ö", "o",
		"ô", "o",
		"õ", "o",
		"ú", "u",
		"ù", "u",
		"ü", "u",
		"û", "u",
		"ñ", "n",
	)

	return replacer.Replace(value)
}
