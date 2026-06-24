package intake

// Este archivo define la unidad de trabajo básica de la etapa de intake.
//
// Su responsabilidad es modelar, en una sola estructura, la identidad del
// archivo detectado y todas las rutas derivadas que el batch necesita a lo
// largo del pipeline.
//
// Así evitamos repartir paths y metadatos del mismo archivo entre varias
// estructuras parciales.
type FileJob struct {
	ProviderID       int
	ProviderName     string
	ProviderEmail    string
	InputPath        string
	RelativePath     string
	ProcessingPath   string
	ProcessedPath    string
	ResultsPath      string
	StructureErrPath string
}
