package reporting

import "time"

// Este archivo define el resultado agregado de un archivo individual.
//
// Su responsabilidad es resumir tanto el estado técnico del archivo como
// las métricas y resultados por fila que deja su procesamiento.
//
// Esta estructura actúa como puente entre procesamiento, reporting,
// notificaciones y escritura de resultados.

// FileStatus enumera los estados posibles de un archivo dentro del batch.
type FileStatus string

const (
	FileStatusPending         FileStatus = "PENDING"
	FileStatusProcessed       FileStatus = "PROCESSED"
	FileStatusProcessedErrors FileStatus = "PROCESSED_WITH_ERRORS"
	FileStatusStructureError  FileStatus = "STRUCTURE_ERROR"
	FileStatusFailed          FileStatus = "FAILED"
)

// FileResult resume cómo terminó el procesamiento de un archivo e incluye
// tanto métricas agregadas de filas como los resultados detallados de la corrida.
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
