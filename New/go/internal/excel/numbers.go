package excel

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
	value := strings.TrimSpace(raw)
	if value == "" {
		return 0, fmt.Errorf("empty numeric value")
	}

	value = strings.ReplaceAll(value, " ", "")
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
		if strings.Count(value, ",") == 1 {
			value = strings.ReplaceAll(value, ",", ".")
		} else {
			value = strings.ReplaceAll(value, ",", "")
		}
	case lastDot >= 0:
		if strings.Count(value, ".") > 1 {
			value = strings.ReplaceAll(value, ".", "")
		}
	}

	parsed, err := strconv.ParseFloat(value, 64)
	if err != nil {
		return 0, fmt.Errorf("parse numeric value %q: %w", raw, err)
	}

	return parsed, nil
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
		return 0, fmt.Errorf("value %q is not an integer", raw)
	}

	return int(floatValue), nil
}
