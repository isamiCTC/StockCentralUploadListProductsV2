package workbook

import (
	"fmt"

	"github.com/xuri/excelize/v2"
)

// Este archivo se encarga de abrir un `.xlsx`, leer su primera hoja y
// transformarlo en una estructura neutral para el resto del sistema.
//
// Responsabilidad del archivo:
// - hablar con `excelize`
// - elegir qué hoja vamos a procesar
// - convertir celdas a una representación propia del proyecto
// - devolver un `Workbook` ya listo para pasar a validación/mapping
//
// No hace todavía lógica de negocio por fila. Solo:
// - abre el archivo
// - identifica la hoja
// - detecta formato
// - normaliza headers
// - arma filas crudas
// - valida estructura

// Reader hoy no mantiene estado propio, pero se modela como struct para
// conservar una superficie simple y permitir extensiones del lector sin
// cambiar su punto de entrada.
type Reader struct{}

// NewReader construye el lector de Excel.
func NewReader() *Reader {
	return &Reader{}
}

// Read abre el workbook y devuelve una estructura ya validada a nivel layout.
//
// Flujo interno:
// 1. abrir el archivo físico
// 2. elegir la primera hoja
// 3. leer todas las filas
// 4. convertir el header a `HeaderCell`
// 5. detectar el formato por cantidad de columnas
// 6. validar estructura
// 7. devolver el `Workbook`
func (r *Reader) Read(path string) (Workbook, error) {
	// `excelize` trabaja sobre el archivo físico del workbook.
	file, err := excelize.OpenFile(path)
	if err != nil {
		return Workbook{}, fmt.Errorf("open xlsx file: %w", err)
	}
	defer func() {
		_ = file.Close()
	}()

	// Tomamos la primera hoja porque ese es el contrato operativo actual
	// del proceso para este tipo de archivos.
	sheets := file.GetSheetList()
	if len(sheets) == 0 {
		return Workbook{}, fmt.Errorf("xlsx file does not contain sheets")
	}

	sheetName := sheets[0]
	// Leemos toda la hoja en memoria para simplificar el procesamiento
	// posterior sobre una representación homogénea del workbook.
	rows, err := file.GetRows(sheetName)
	if err != nil {
		return Workbook{}, fmt.Errorf("read rows from sheet %s: %w", sheetName, err)
	}

	// Si no hay filas, devolvemos un workbook con error estructural en lugar
	// de fallar con un error técnico genérico.
	if len(rows) == 0 {
		return Workbook{
			Path:      path,
			SheetName: sheetName,
			Format:    FileFormatUnknown,
			Rows:      []RawRow{},
			StructureErrors: []StructureError{
				{
					Type:    "ESTRUCTURA",
					Field:   "ARCHIVO",
					Message: "El archivo está vacío",
					Detail:  "",
				},
			},
		}, nil
	}

	// La primera fila se interpreta como header.
	headers := buildHeaders(rows[0])
	format := DetectFormat(len(headers))

	// Esta validación es deliberadamente gruesa: primero detectamos si el
	// archivo entra en algún formato conocido, y recién después refinamos.
	// Si la cantidad de columnas no encaja en ninguno de los formatos válidos,
	// no intentamos seguir con una validación más fina.
	if format == FileFormatUnsupported {
		return Workbook{
			Path:      path,
			SheetName: sheetName,
			Format:    format,
			Headers:   headers,
			Rows:      buildRows(rows[1:]),
			StructureErrors: []StructureError{
				{
					Type:    "ESTRUCTURA",
					Field:   "FORMATO",
					Message: "Cantidad de columnas no soportada",
					Detail:  fmt.Sprintf("Se detectaron %d columnas", len(headers)),
				},
			},
		}, nil
	}

	// Si el formato general es válido, ahora sí construimos el índice de
	// columnas y los posibles errores estructurales detallados.
	headerIndexByKey, structureErrors := ValidateStructure(format, headers)

	// El workbook que sale de acá ya está listo para pasar al mapper.
	return Workbook{
		Path:             path,
		SheetName:        sheetName,
		Format:           format,
		Headers:          headers,
		HeaderIndexByKey: headerIndexByKey,
		Rows:             buildRows(rows[1:]),
		StructureErrors:  structureErrors,
	}, nil
}

// buildHeaders convierte la fila 1 del Excel en metadatos de encabezado.
//
// Cada celda del header se conserva:
// - limpia en su forma original
// - y normalizada para comparación
func buildHeaders(values []string) []HeaderCell {
	headers := make([]HeaderCell, 0, len(values))
	for index, value := range values {
		// Guardamos tanto la forma "visible" como la forma normalizada
		// para comparar headers de manera robusta.
		clean := NormalizeCell(value)
		headers = append(headers, HeaderCell{
			Index:      index,
			Original:   clean,
			Normalized: NormalizeHeader(clean),
		})
	}
	return headers
}

// buildRows transforma las filas restantes del workbook en filas crudas.
//
// No valida negocio. Solo prepara el material para el mapper:
// - limpia celdas
// - marca filas totalmente vacías
// - conserva el número real de fila de Excel
func buildRows(sourceRows [][]string) []RawRow {
	rows := make([]RawRow, 0, len(sourceRows))

	for index, values := range sourceRows {
		normalizedValues := make([]string, len(values))
		isEmpty := true

		// Si después de limpiar todas las celdas la fila no tiene contenido,
		// la marcamos como vacía para que etapas posteriores la puedan omitir.
		for i, value := range values {
			normalizedValues[i] = NormalizeCell(value)
			if normalizedValues[i] != "" {
				isEmpty = false
			}
		}

		// `index + 2` porque sourceRows ya no incluye el header y Excel arranca en 1.
		rows = append(rows, RawRow{
			ExcelRowNumber: index + 2,
			Values:         normalizedValues,
			IsEmpty:        isEmpty,
		})
	}

	return rows
}
