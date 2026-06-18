package reporting

import "time"

// BatchResult consolida el resultado global de una corrida completa.
// Es la estructura que vuelve al final del orquestador principal.
type BatchResult struct {
	StartedAt       time.Time
	FinishedAt      time.Time
	ProvidersSeen   int
	ProvidersActive int
	FilesDetected   int
	FilesProcessed  int
	FilesFailed     int
	Files           []FileResult
}
