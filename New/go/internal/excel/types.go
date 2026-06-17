package excel

// Este archivo reúne los tipos base de la capa Excel.
//
// Responsabilidad del archivo:
// - definir los conceptos centrales que nacen de un `.xlsx`
// - ofrecer nombres claros para el resto del sistema
// - evitar que otros paquetes dependan de detalles de `excelize`
//
// En otras palabras, este archivo traduce el mundo "Excel" a un lenguaje
// propio del proyecto: workbook, header, fila cruda, formato y errores
// de estructura.

// FileFormat representa los formatos de archivo reconocidos por la V2.
//
// Hoy solo existen dos formatos funcionales válidos:
// - `STOCK_UPDATE`: archivo chico de actualización de stock
// - `FULL_IMPORT`: archivo completo de producto
//
// Todo lo demás cae en `UNSUPPORTED`.
type FileFormat string

const (
	FileFormatUnknown     FileFormat = "UNKNOWN"
	FileFormatStockUpdate FileFormat = "STOCK_UPDATE"
	FileFormatFullImport  FileFormat = "FULL_IMPORT"
	FileFormatUnsupported FileFormat = "UNSUPPORTED"
)

// StructureError modela un problema de layout del Excel.
//
// Esto no representa errores de una fila puntual, sino problemas del archivo
// como archivo:
// - falta una columna obligatoria
// - hay columnas duplicadas
// - la cantidad de columnas no corresponde a ningún formato válido
//
// Estos errores son los que luego deberían terminar en la hoja
// `ErroresEstructura`.
type StructureError struct {
	Type    string
	Field   string
	Message string
	Detail  string
}

// HeaderCell representa una columna detectada en el header.
//
// Guardamos dos versiones del mismo dato:
// - `Original`: cómo vino realmente en el Excel
// - `Normalized`: cómo lo vamos a comparar internamente
//
// Eso nos permite ser laxos al validar, pero sin perder trazabilidad
// del valor original que venía en el archivo.
type HeaderCell struct {
	Index      int
	Original   string
	Normalized string
}

// RawRow representa una fila tal como viene del Excel, pero ya convertida
// a strings y con metadatos útiles para el procesamiento posterior.
//
// En esta etapa todavía no sabemos si la fila es válida para negocio.
// Solo sabemos:
// - en qué número de fila de Excel estaba
// - cuáles eran sus celdas normalizadas
// - si estaba completamente vacía
type RawRow struct {
	ExcelRowNumber int
	Values         []string
	IsEmpty        bool
}

// Workbook es el resultado principal de leer y validar un Excel.
//
// Esta estructura es el "paquete completo" que sale del reader:
// - identidad del archivo
// - hoja tomada
// - formato detectado
// - columnas
// - filas crudas
// - errores estructurales, si los hubo
type Workbook struct {
	Path             string
	SheetName        string
	Format           FileFormat
	Headers          []HeaderCell
	HeaderIndexByKey map[string]int
	Rows             []RawRow
	StructureErrors  []StructureError
}

// IsStructureValid indica si el archivo pasó la validación de layout.
//
// Es un helper chico, pero muy útil para que el flujo superior se lea fácil:
// `if !workbook.IsStructureValid() { ... }`
func (w Workbook) IsStructureValid() bool {
	return len(w.StructureErrors) == 0
}
