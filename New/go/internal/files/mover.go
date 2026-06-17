package files

import (
	"fmt"
	"os"
	"path/filepath"

	"stockcentraluploadlistproductsv2/internal/domain"
)

// Este archivo centraliza la lógica de rutas y movimientos de archivos.
//
// Tener esto aislado evita repartir `os.Rename`, `filepath.Join` y reglas
// de nombres por todo el proyecto.
type Mover struct {
	processingRoot string
	processedRoot  string
}

// NewMover recibe las dos raíces administradas por el batch.
func NewMover(processingRoot, processedRoot string) *Mover {
	return &Mover{
		processingRoot: processingRoot,
		processedRoot:  processedRoot,
	}
}

// BuildPaths completa en el `FileJob` todas las rutas derivadas.
// No mueve nada todavía: solo calcula.
func (m *Mover) BuildPaths(job domain.FileJob) domain.FileJob {
	providerDir := fmt.Sprintf("%d", job.ProviderID)

	// Conservamos la subestructura relativa del archivo dentro de cada raíz.
	job.ProcessingPath = filepath.Join(m.processingRoot, providerDir, job.RelativePath)
	job.ProcessedPath = filepath.Join(m.processedRoot, providerDir, job.RelativePath)

	// A partir del nombre original armamos los nombres de salida que el batch
	// deja junto al archivo ya procesado.
	baseName := stringsTrimExt(filepath.Base(job.RelativePath))
	resultsName := baseName + ".result.xlsx"
	structureName := baseName + ".structure-errors.xlsx"

	job.ResultsPath = filepath.Join(m.processedRoot, providerDir, filepath.Dir(job.RelativePath), resultsName)
	job.StructureErrPath = filepath.Join(m.processedRoot, providerDir, filepath.Dir(job.RelativePath), structureName)

	return job
}

// MoveToProcessing mueve el archivo desde input hacia processing.
// Primero crea la carpeta padre si hace falta.
func (m *Mover) MoveToProcessing(job domain.FileJob) (domain.FileJob, error) {
	// Si la carpeta todavía no existe, la creamos antes de intentar mover.
	if err := ensureParent(job.ProcessingPath); err != nil {
		return job, fmt.Errorf("prepare processing destination: %w", err)
	}
	// El rename deja el archivo físicamente en la carpeta de trabajo del batch.
	if err := os.Rename(job.InputPath, job.ProcessingPath); err != nil {
		return job, fmt.Errorf("move file to processing: %w", err)
	}

	// Actualizamos la ruta viva del job para que el resto del flujo use
	// siempre la ubicación más reciente del archivo.
	job.InputPath = job.ProcessingPath
	return job, nil
}

// MoveToProcessed mueve el archivo desde processing hacia processed.
func (m *Mover) MoveToProcessed(job domain.FileJob) (domain.FileJob, error) {
	// Igual que en processing, primero garantizamos la carpeta destino.
	if err := ensureParent(job.ProcessedPath); err != nil {
		return job, fmt.Errorf("prepare processed destination: %w", err)
	}
	// Este paso cierra el ciclo del archivo y lo saca del directorio temporal.
	if err := os.Rename(job.InputPath, job.ProcessedPath); err != nil {
		return job, fmt.Errorf("move file to processed: %w", err)
	}

	job.InputPath = job.ProcessedPath
	return job, nil
}

// ensureParent crea la carpeta padre de una ruta final.
func ensureParent(path string) error {
	return os.MkdirAll(filepath.Dir(path), 0o755)
}

// stringsTrimExt devuelve el nombre base sin extensión.
func stringsTrimExt(name string) string {
	ext := filepath.Ext(name)
	return name[:len(name)-len(ext)]
}
