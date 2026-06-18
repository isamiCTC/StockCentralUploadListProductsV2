package batch

import (
	"context"
	"fmt"
	"slices"
	"time"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/intake"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/providers"
	"stockcentraluploadlistproductsv2/internal/reporting"
)

// Este archivo contiene el orquestador principal del batch.
//
// Sus responsabilidades son:
// - pedir providers válidos
// - descubrir archivos elegibles
// - procesar cada archivo de a uno
// - consolidar métricas globales
type Processor struct {
	cfg           appconfig.BatchConfig
	providerRepo  providers.ProviderRepository
	scanner       *intake.Scanner
	fileProcessor *FileProcessor
	logs          logging.LoggerSet
}

// NewProcessor construye el orquestador del batch.
func NewProcessor(
	cfg appconfig.BatchConfig,
	providerRepo providers.ProviderRepository,
	scanner *intake.Scanner,
	fileProcessor *FileProcessor,
	logs logging.LoggerSet,
) *Processor {
	return &Processor{
		cfg:           cfg,
		providerRepo:  providerRepo,
		scanner:       scanner,
		fileProcessor: fileProcessor,
		logs:          logs,
	}
}

// Run ejecuta una corrida completa del batch de principio a fin.
func (p *Processor) Run(ctx context.Context) (reporting.BatchResult, error) {
	// Desde este momento empezamos a medir la corrida global.
	result := reporting.BatchResult{StartedAt: time.Now()}

	// Paso 1. Traer providers válidos desde la fuente configurada.
	// Esto define qué carpetas del input son "legítimas" para esta corrida.
	providerList, err := p.providerRepo.ListEnabledByIntegratorAndCatalog(ctx, p.cfg.ProviderIntegratorID, p.cfg.CatalogID)
	if err != nil {
		return result, fmt.Errorf("list providers from repository: %w", err)
	}

	// Ordenamos por ID para tener un orden estable y más fácil de auditar.
	// Así dos corridas equivalentes recorren providers en el mismo orden.
	slices.SortFunc(providerList, func(a, b providers.Provider) int {
		switch {
		case a.ID < b.ID:
			return -1
		case a.ID > b.ID:
			return 1
		default:
			return 0
		}
	})

	result.ProvidersSeen = len(providerList)
	result.ProvidersActive = len(providerList)

	// Dejamos auditado cuántos providers participaron efectivamente.
	p.logs.Summary.Info("providers-loaded",
		logging.Int("catalog_id", p.cfg.CatalogID),
		logging.Int("provider_integrator_id", p.cfg.ProviderIntegratorID),
		logging.Int("providers_count", len(providerList)),
	)

	// Paso 2. Buscar archivos dentro de carpetas válidas.
	// Acá todavía no procesamos nada: solo descubrimos trabajo pendiente.
	jobs, err := p.scanner.DiscoverProviderFiles(ctx, providerList)
	if err != nil {
		return result, fmt.Errorf("discover provider files: %w", err)
	}

	result.FilesDetected = len(jobs)
	p.logs.Summary.Info("files-discovered", logging.Int("count", len(jobs)))

	// Paso 3. Procesar cada archivo de forma secuencial.
	// Elegimos secuencialidad por archivo para que el movimiento de archivos,
	// logs y resultados finales sean más predecibles.
	// La concurrencia real vive dentro de cada archivo, a nivel fila.
	for _, job := range jobs {
		// Cada archivo se procesa completo y devuelve un resumen propio.
		fileResult, fileErr := p.fileProcessor.Process(ctx, job)
		result.Files = append(result.Files, fileResult)

		if fileErr != nil {
			// Si el archivo explotó a nivel técnico, lo contamos aparte.
			// Esto es distinto a "tuvo filas con error": acá falló el archivo
			// como unidad de trabajo.
			result.FilesFailed++
			p.logs.Summary.Error("file-failed",
				logging.Int("provider_id", job.ProviderID),
				logging.String("file", job.RelativePath),
				logging.String("error", fileErr.Error()),
			)

			// Si la config pide cortar en el primer error, salimos acá.
			if p.cfg.StopOnFileError {
				result.FinishedAt = time.Now()
				return result, fmt.Errorf("stop_on_file_error active: %w", fileErr)
			}
			continue
		}

		// Un archivo puede terminar con filas en error, pero igual contar como
		// "procesado" si el flujo técnico del archivo terminó.
		result.FilesProcessed++
	}

	// Cerramos el resultado del batch con timestamp final.
	result.FinishedAt = time.Now()
	return result, nil
}
