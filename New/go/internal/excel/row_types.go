package excel

// Este archivo define los DTOs tipados que salen de la etapa de mapeo.
//
// Responsabilidad del archivo:
// - expresar qué "formas" de fila entiende el sistema
// - separar claramente fila cruda de fila validada
// - dejar los datos listos para la lógica de negocio y API

// RowIssue representa un problema detectado al mapear una fila.
//
// No distingue todavía entre warning y partial de manera compleja, pero deja
// la base para que el resultado final pueda explicar por qué una fila falló.
type RowIssue struct {
	Severity string
	Field    string
	Message  string
	Detail   string
}

// StockUpdateRow es el DTO del formato de 2 columnas.
// Su intención es muy acotada: solo actualizar stock.
type StockUpdateRow struct {
	SKU   string
	Stock int
}

// FullImportRow es el DTO del formato completo de 19 columnas.
//
// Ya incorpora conversiones y normalizaciones clave del legado, por ejemplo:
// - peso en gramos y también en kilogramos
// - oferta aplicada sobre `Price`
// - URLs de imágenes ya separadas y filtradas
// - `ShortDescription` preparada
type FullImportRow struct {
	SKU              string
	Name             string
	Brand            string
	Description      string
	ShortDescription string
	Height           float64
	Width            float64
	Depth            float64
	WeightGrams      float64
	WeightKilograms  float64
	ImageURLs        []string
	HasImages        bool
	Price            float64
	ListPrice        float64
	NetPrice         float64
	Taxes            float64
	Type             string
	Ahora            string
	Category         string
	SubCategory      string
	Stock            int
	Offer            *float64
	OfferApplied     bool
	StartDateRaw     string
	EndDateRaw       string
	SyncImages       bool
}

// MappedRow representa el resultado de mapear una fila concreta.
//
// Solo una de las dos variantes de payload debería venir poblada:
// - `StockUpdate`
// - `FullImport`
//
// Si la fila viene vacía o con errores, puede no tener ninguna de las dos.
type MappedRow struct {
	ExcelRowNumber int
	SKU            string
	IsEmpty        bool
	StockUpdate    *StockUpdateRow
	FullImport     *FullImportRow
	Issues         []RowIssue
}

// HasErrors indica si la fila tiene al menos un issue de severidad error.
// Esto simplifica bastante el flujo posterior del batch.
func (r MappedRow) HasErrors() bool {
	for _, issue := range r.Issues {
		if issue.Severity == "ERROR" {
			return true
		}
	}
	return false
}
