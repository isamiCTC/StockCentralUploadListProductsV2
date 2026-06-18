package intake

// FileJob representa un archivo detectado listo para entrar al pipeline.
//
// Contiene tanto la identidad lógica del archivo como las rutas que va
// tomando a medida que atraviesa input, processing y processed.
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
