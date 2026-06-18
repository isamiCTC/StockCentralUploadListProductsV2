package workbook

import "fmt"

// Este archivo concentra las definiciones de formatos soportados y sus
// columnas requeridas.
//
// Responsabilidad del archivo:
// - decidir qué formatos existen
// - listar qué columnas exige cada formato
// - mantener en un solo lugar las reglas de layout "macro"
//
// Aunque el legado usa posiciones fijas, en V2 vamos a validar por existencia
// de columnas obligatorias con matching laxo, manteniendo la decisión principal
// por cantidad de columnas.

const (
	stockUpdateColumnCount = 2
	fullImportColumnCount  = 19
)

var stockUpdateColumns = []string{
	"SKU",
	"STOCK",
}

// fullImportColumns conserva el layout funcional del legado.
// Este orden refleja las 19 columnas que el servicio original espera
// para una importación completa.
var fullImportColumns = []string{
	"SKU",
	"NOMBRE",
	"MARCA",
	"DESCRIPCION",
	"ALTO",
	"ANCHO",
	"LARGO",
	"PESO",
	"URL IMAGENES",
	"PRECIO",
	"IVA",
	"TIPO",
	"AHORA",
	"CATEGORIA",
	"SUB CATEGORIA",
	"STOCK",
	"OFERTA",
	"FECHA DE INICIO",
	"FECHA DE FIN",
}

// DetectFormat decide qué semántica de archivo aplicar según cantidad de columnas.
//
// Esta decisión imita el criterio central del legacy:
// primero se mira la forma general del archivo y recién después se valida
// su contenido más fino.
func DetectFormat(columnCount int) FileFormat {
	switch columnCount {
	case stockUpdateColumnCount:
		return FileFormatStockUpdate
	case fullImportColumnCount:
		return FileFormatFullImport
	default:
		return FileFormatUnsupported
	}
}

// RequiredColumns devuelve el catálogo de columnas obligatorias para el formato.
//
// Si el formato no es soportado, devolvemos error porque no hay ninguna
// lista de columnas contra la cual validar.
func RequiredColumns(format FileFormat) ([]string, error) {
	switch format {
	case FileFormatStockUpdate:
		return stockUpdateColumns, nil
	case FileFormatFullImport:
		return fullImportColumns, nil
	default:
		return nil, fmt.Errorf("format %s does not define required columns", format)
	}
}
