package reporting

import "time"

// Este archivo define el resultado agregado de una corrida completa del batch.
//
// Su responsabilidad es concentrar las métricas globales que produce el
// orquestador principal para logging, diagnóstico y salida final del proceso.
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
