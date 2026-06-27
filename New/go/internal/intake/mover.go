package intake

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// Este archivo centraliza la lógica de rutas y movimientos de archivos.
//
// Tener esto aislado evita repartir `os.Rename`, `filepath.Join` y reglas
// de nombres por todo el proyecto.
type Mover struct {
	processingRoot string
	processedRoot  string
	renameFile     func(oldPath, newPath string) error
	now            func() time.Time
}

// NewMover recibe las dos raíces administradas por el batch.
func NewMover(processingRoot, processedRoot string) *Mover {
	return &Mover{
		processingRoot: processingRoot,
		processedRoot:  processedRoot,
		renameFile:     os.Rename,
		now:            time.Now,
	}
}

// BuildPaths completa en el `FileJob` todas las rutas derivadas.
// No mueve nada todavía: solo calcula.
func (m *Mover) BuildPaths(job FileJob) FileJob {
	providerDir := fmt.Sprintf("%d", job.ProviderID)

	// Conservamos la subestructura relativa del archivo dentro de cada raíz.
	job.ProcessingPath = filepath.Join(m.processingRoot, providerDir, job.RelativePath)

	// Las rutas finales en `processed` se calculan recién al cerrar el archivo
	// para poder sellarlas con un timestamp real de procesamiento.
	job.ProcessedPath = ""
	job.ResultsPath = ""
	job.StructureErrPath = ""

	return job
}

// MoveToProcessing mueve el archivo desde input hacia processing.
// Primero crea la carpeta padre si hace falta.
func (m *Mover) MoveToProcessing(job FileJob) (FileJob, error) {
	// Si la carpeta todavía no existe, la creamos antes de intentar mover.
	if err := ensureParent(job.ProcessingPath); err != nil {
		return job, fmt.Errorf("prepare processing destination: %w", err)
	}
	// Intentamos primero el camino barato: rename directo.
	if err := moveFile(job.InputPath, job.ProcessingPath, m.renameFile); err != nil {
		return job, fmt.Errorf("move file to processing: %w", err)
	}

	// Actualizamos la ruta viva del job para que el resto del flujo use
	// siempre la ubicación más reciente del archivo.
	job.InputPath = job.ProcessingPath
	return job, nil
}

// MoveToProcessed mueve el archivo desde processing hacia processed.
func (m *Mover) MoveToProcessed(job FileJob) (FileJob, error) {
	job = m.buildProcessedPaths(job)

	// Igual que en processing, primero garantizamos la carpeta destino.
	if err := ensureParent(job.ProcessedPath); err != nil {
		return job, fmt.Errorf("prepare processed destination: %w", err)
	}
	// Igual que en processing, si no se puede renombrar entre unidades,
	// hacemos fallback a copiar y borrar.
	if err := moveFile(job.InputPath, job.ProcessedPath, m.renameFile); err != nil {
		return job, fmt.Errorf("move file to processed: %w", err)
	}

	job.InputPath = job.ProcessedPath
	return job, nil
}

// buildProcessedPaths arma las rutas finales usando el nombre original más un
// timestamp estable para evitar colisiones y mejorar trazabilidad.
func (m *Mover) buildProcessedPaths(job FileJob) FileJob {
	providerDir := fmt.Sprintf("%d", job.ProviderID)
	relativeDir := filepath.Dir(job.RelativePath)
	originalName := filepath.Base(job.RelativePath)
	baseName := stringsTrimExt(originalName)
	extension := filepath.Ext(originalName)
	timestamp := m.now().Format("20060102_150405")
	stampedBaseName := baseName + "__" + timestamp

	job.ProcessedPath = filepath.Join(m.processedRoot, providerDir, relativeDir, stampedBaseName+extension)
	job.ResultsPath = filepath.Join(m.processedRoot, providerDir, relativeDir, stampedBaseName+".result.xlsx")
	job.StructureErrPath = filepath.Join(m.processedRoot, providerDir, relativeDir, stampedBaseName+".structure-errors.xlsx")

	return job
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

// moveFile intenta primero `rename` y, si el filesystem no permite mover
// entre volúmenes/unidades, hace fallback a copiar y borrar el origen.
func moveFile(sourcePath, destinationPath string, renameFile func(oldPath, newPath string) error) error {
	if err := renameFile(sourcePath, destinationPath); err == nil {
		return nil
	} else if !isCrossDeviceMoveError(err) {
		return err
	}

	return copyAndRemove(sourcePath, destinationPath)
}

// isCrossDeviceMoveError detecta mensajes típicos de rename entre volúmenes
// distintos, tanto en Unix (`cross-device link`) como en Windows
// (`otra unidad de disco` / `different disk drive`).
func isCrossDeviceMoveError(err error) bool {
	if err == nil {
		return false
	}

	message := strings.ToLower(err.Error())
	return strings.Contains(message, "cross-device link") ||
		strings.Contains(message, "otra unidad de disco") ||
		strings.Contains(message, "different disk drive")
}

// copyAndRemove replica un move físico cuando rename no es posible.
func copyAndRemove(sourcePath, destinationPath string) error {
	sourceFile, err := os.Open(sourcePath)
	if err != nil {
		return fmt.Errorf("open source file: %w", err)
	}

	info, err := sourceFile.Stat()
	if err != nil {
		_ = sourceFile.Close()
		return fmt.Errorf("stat source file: %w", err)
	}

	destinationFile, err := os.Create(destinationPath)
	if err != nil {
		_ = sourceFile.Close()
		return fmt.Errorf("create destination file: %w", err)
	}

	copyErr := func() error {
		if _, err := io.Copy(destinationFile, sourceFile); err != nil {
			return fmt.Errorf("copy file contents: %w", err)
		}
		if err := destinationFile.Sync(); err != nil {
			return fmt.Errorf("sync destination file: %w", err)
		}
		if err := os.Chmod(destinationPath, info.Mode()); err != nil {
			return fmt.Errorf("chmod destination file: %w", err)
		}
		return nil
	}()
	closeDestinationErr := destinationFile.Close()
	closeSourceErr := sourceFile.Close()
	if copyErr != nil {
		_ = os.Remove(destinationPath)
		return copyErr
	}
	if closeDestinationErr != nil {
		_ = os.Remove(destinationPath)
		return fmt.Errorf("close destination file: %w", closeDestinationErr)
	}
	if closeSourceErr != nil {
		_ = os.Remove(destinationPath)
		return fmt.Errorf("close source file: %w", closeSourceErr)
	}

	if err := os.Remove(sourcePath); err != nil {
		_ = os.Remove(destinationPath)
		return fmt.Errorf("remove source file after copy: %w", err)
	}

	return nil
}
