package workbook

import (
	"fmt"
	"math"
	"strconv"
	"strings"
)

// Este archivo concentra el parsing numérico flexible acordado para V2.
//
// Responsabilidad del archivo:
// - aceptar formatos numéricos comunes de proveedores
// - reducir la fragilidad ante coma/punto/miles
// - seguir siendo estricto cuando el valor es realmente ambiguo o inválido
//
// Esta capa es importante porque gran parte de los errores del Excel suelen
// vivir en precios, dimensiones, IVA, stock y peso.

// ParseFlexibleFloat intenta interpretar números con formatos comunes:
// - 1234.56
// - 1234,56
// - 1.234,56
// - 1,234.56
//
// Estrategia:
// - quitar espacios
// - detectar cuál parece ser el separador decimal
// - remover el separador de miles
// - convertir al formato que Go espera para ParseFloat
func ParseFlexibleFloat(raw string) (float64, error) {
	value := normalizeNumericInput(raw)
	if value == "" {
		return 0, fmt.Errorf("el valor numérico está vacío")
	}

	lastDot := strings.LastIndex(value, ".")
	lastComma := strings.LastIndex(value, ",")

	// Si existen punto y coma a la vez, tomamos como decimal el último
	// separador visible. Es una heurística común y razonable para este caso.
	switch {
	case lastDot >= 0 && lastComma >= 0:
		if lastDot > lastComma {
			value = strings.ReplaceAll(value, ",", "")
		} else {
			value = strings.ReplaceAll(value, ".", "")
			value = strings.ReplaceAll(value, ",", ".")
		}
	case lastComma >= 0:
		// Con un solo separador distinguimos un grupo de miles (1,000)
		// de una fraccion decimal (12,5 o 0,125).
		if strings.Count(value, ",") == 1 {
			if isSingleThousandsSeparator(value, ",") {
				value = strings.ReplaceAll(value, ",", "")
			} else {
				value = strings.ReplaceAll(value, ",", ".")
			}
		} else {
			value = strings.ReplaceAll(value, ",", "")
		}
	case lastDot >= 0:
		// Varias apariciones de punto se interpretan como miles. Un unico
		// punto con tres digitos a la derecha tambien puede ser miles.
		if strings.Count(value, ".") > 1 || isSingleThousandsSeparator(value, ".") {
			value = strings.ReplaceAll(value, ".", "")
		}
	}

	// Una vez normalizado, ya podemos usar el parser estándar de Go.
	parsed, err := strconv.ParseFloat(value, 64)
	if err != nil {
		return 0, fmt.Errorf("formato numérico inválido")
	}

	return parsed, nil
}

// isSingleThousandsSeparator resuelve el caso ambiguo de un unico punto o
// coma. Lo considera miles cuando hay de uno a tres digitos enteros distintos
// de cero a la izquierda y exactamente tres digitos a la derecha.
//
// El cero inicial queda reservado para fracciones: 0.125 sigue siendo 0.125.
func isSingleThousandsSeparator(value, separator string) bool {
	parts := strings.Split(value, separator)
	if len(parts) != 2 {
		return false
	}

	integerPart := strings.TrimPrefix(parts[0], "+")
	integerPart = strings.TrimPrefix(integerPart, "-")
	return len(integerPart) >= 1 && len(integerPart) <= 3 && integerPart != "0" &&
		len(parts[1]) == 3 && allDigits(integerPart) && allDigits(parts[1])
}

func allDigits(value string) bool {
	if value == "" {
		return false
	}
	for _, char := range value {
		if char < '0' || char > '9' {
			return false
		}
	}
	return true
}

// ParseFlexibleInt intenta leer un entero desde formatos numéricos razonables.
//
// Primero parsea como float y después verifica que no tenga parte decimal real.
// Si la tiene, preferimos fallar antes que truncar silenciosamente.
func ParseFlexibleInt(raw string) (int, error) {
	floatValue, err := ParseFlexibleFloat(raw)
	if err != nil {
		return 0, err
	}

	if math.Mod(floatValue, 1) != 0 {
		return 0, fmt.Errorf("el valor debe ser un número entero sin decimales")
	}

	return int(floatValue), nil
}

// normalizeNumericInput limpia variantes frecuentes en celdas numéricas.
//
// Hoy toleramos:
// - espacios alrededor y entre miles
// - símbolo `$` pegado o separado del número
func normalizeNumericInput(raw string) string {
	value := strings.TrimSpace(raw)
	value = strings.ReplaceAll(value, "$", "")
	value = strings.ReplaceAll(value, " ", "")
	return value
}
