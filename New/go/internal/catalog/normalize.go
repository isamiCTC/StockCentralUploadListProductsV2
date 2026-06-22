package catalog

import (
	"strings"
	"unicode"
)

// normalizeCategoryKey arma la clave de comparación interna usada por el
// resolvedor para matchear subcategorías contra el hardcode.
func normalizeCategoryKey(value string) string {
	trimmed := strings.TrimSpace(value)
	upper := strings.ToUpper(trimmed)
	withoutAccents := removeCategoryAccents(upper)
	return collapseCategoryWhitespace(withoutAccents)
}

// collapseCategoryWhitespace reduce cualquier secuencia de blancos a un solo
// espacio para que el match hardcodeado tolere variaciones de carga.
func collapseCategoryWhitespace(value string) string {
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

// removeCategoryAccents elimina tildes y variantes comunes para que la
// comparación con el hardcode sea más laxa, sin tocar el valor original.
func removeCategoryAccents(value string) string {
	replacer := strings.NewReplacer(
		"Á", "A",
		"À", "A",
		"Ä", "A",
		"Â", "A",
		"Ã", "A",
		"É", "E",
		"È", "E",
		"Ë", "E",
		"Ê", "E",
		"Í", "I",
		"Ì", "I",
		"Ï", "I",
		"Î", "I",
		"Ó", "O",
		"Ò", "O",
		"Ö", "O",
		"Ô", "O",
		"Õ", "O",
		"Ú", "U",
		"Ù", "U",
		"Ü", "U",
		"Û", "U",
		"Ñ", "N",
	)

	return replacer.Replace(value)
}
