package reporting

// Este archivo define el resultado funcional de una fila individual.
//
// La idea es separar claramente:
// - el resultado técnico del archivo completo
// - el resultado de cada SKU/fila dentro de ese archivo
//
// Esto luego nos permite:
// - escribir el Excel de `Resultados`
// - adjuntarlo por mail
// - consolidar métricas sin volver a inferir estados a partir de logs

// RowStatus enumera los estados finales posibles de una fila.
type RowStatus string

const (
	RowStatusOK        RowStatus = "OK"
	RowStatusPartialOK RowStatus = "PARTIAL_OK"
	RowStatusError     RowStatus = "ERROR"
	RowStatusSkipped   RowStatus = "SKIPPED"
)

// RowResult representa el resultado legible de una fila procesada.
type RowResult struct {
	ProviderID     int
	ExcelRowNumber int
	SKU            string
	Status         RowStatus
	ProductResult  string
	ImagesResult   string
	Message        string
	Detail         string
}

// IsError expone si la fila terminó en error duro.
func (r RowResult) IsError() bool {
	return r.Status == RowStatusError
}

// IsPartial expone si la fila terminó parcialmente bien.
func (r RowResult) IsPartial() bool {
	return r.Status == RowStatusPartialOK
}

// IsSkipped expone si la fila fue omitida sin error técnico.
func (r RowResult) IsSkipped() bool {
	return r.Status == RowStatusSkipped
}
