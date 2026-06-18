package workbook

import "fmt"

// Este archivo contiene la validación estructural del workbook.
//
// Responsabilidad del archivo:
// - revisar si el archivo "tiene forma" de archivo procesable
// - construir un índice de columnas reutilizable para el mapper
// - devolver errores entendibles para negocio y soporte
//
// La validación responde dos preguntas:
// 1. la cantidad de columnas corresponde a un formato soportado
// 2. las columnas obligatorias existen realmente en el header

// ValidateStructure revisa el layout del archivo y devuelve:
// - el índice de columnas por nombre normalizado
// - la lista de errores estructurales encontrados
//
// Este índice es importante porque evita seguir buscando columnas por texto
// una y otra vez en etapas posteriores.
func ValidateStructure(format FileFormat, headers []HeaderCell) (map[string]int, []StructureError) {
	// Si no hay headers, no tiene sentido seguir validando nada más.
	if len(headers) == 0 {
		return nil, []StructureError{
			{
				Type:    "ESTRUCTURA",
				Field:   "HEADER",
				Message: "El archivo no tiene fila de encabezados",
				Detail:  "",
			},
		}
	}

	// Si el formato no tiene definición de columnas obligatorias, el problema
	// es de formato general, no de un header puntual.
	requiredColumns, err := RequiredColumns(format)
	if err != nil {
		return nil, []StructureError{
			{
				Type:    "ESTRUCTURA",
				Field:   "FORMATO",
				Message: "Formato de archivo no soportado",
				Detail:  err.Error(),
			},
		}
	}

	// Armamos un mapa para poder encontrar columnas por clave normalizada.
	// En paralelo detectamos duplicados relevantes.
	headerIndexByKey := make(map[string]int, len(headers))
	duplicates := make(map[string]int)

	for _, header := range headers {
		// Si el mismo nombre normalizado ya apareció, lo marcamos como duplicado
		// y no pisamos la primera posición encontrada.
		if _, exists := headerIndexByKey[header.Normalized]; exists {
			duplicates[header.Normalized]++
			continue
		}
		// Guardamos la posición real de la columna para reutilizarla después
		// cuando haya que leer celdas por nombre lógico.
		headerIndexByKey[header.Normalized] = header.Index
	}

	var errors []StructureError

	// Revisamos una por una las columnas que el formato exige.
	for _, required := range requiredColumns {
		key := NormalizeHeader(required)
		// Si no encontramos una columna obligatoria, el archivo puede existir
		// pero no tiene el layout que espera el proceso.
		if _, ok := headerIndexByKey[key]; !ok {
			errors = append(errors, StructureError{
				Type:    "ESTRUCTURA",
				Field:   required,
				Message: "Columna obligatoria faltante",
				Detail:  "",
			})
		}
	}

	// También informamos duplicados porque después harían ambiguo el mapping.
	for normalizedKey, count := range duplicates {
		// Reportamos cuántas veces extra aparece para que sea más fácil corregir
		// el workbook original.
		errors = append(errors, StructureError{
			Type:    "ESTRUCTURA",
			Field:   normalizedKey,
			Message: "Columna duplicada",
			Detail:  fmt.Sprintf("La columna aparece %d veces adicionales", count),
		})
	}

	return headerIndexByKey, errors
}
