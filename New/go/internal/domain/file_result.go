package domain

import "time"

// FileStatus enumera los estados posibles de un archivo dentro del batch.
type FileStatus string

const (
	FileStatusPending         FileStatus = "PENDING"
	FileStatusProcessed       FileStatus = "PROCESSED"
	FileStatusProcessedErrors FileStatus = "PROCESSED_WITH_ERRORS"
	FileStatusStructureError  FileStatus = "STRUCTURE_ERROR"
	FileStatusFailed          FileStatus = "FAILED"
)

// FileResult resume cómo terminó el procesamiento de un archivo.
// Más adelante se va a completar con métricas reales de filas y resultados.
type FileResult struct {
	ProviderName        string
	ProviderEmail       string
	ProviderID          int
	InputPath           string
	ProcessingPath      string
	ProcessedPath       string
	Status              FileStatus
	DetectedRows        int
	ProcessedRows       int
	SkippedRows         int
	SuccessfulRows      int
	PartialRows         int
	ErrorRows           int
	StartedAt           time.Time
	FinishedAt          time.Time
	FailureReason       string
	ResultsFilePath     string
	StructureErrorsPath string
	RowResults          []RowResult
}
