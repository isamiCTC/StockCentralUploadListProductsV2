package runbatch

import (
	"fmt"
	"time"

	"stockcentraluploadlistproductsv2/internal/batch"
	"stockcentraluploadlistproductsv2/internal/catalog"
	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/images"
	"stockcentraluploadlistproductsv2/internal/intake"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/notifications"
	"stockcentraluploadlistproductsv2/internal/products"
	"stockcentraluploadlistproductsv2/internal/providers"
	"stockcentraluploadlistproductsv2/internal/reporting"
	"stockcentraluploadlistproductsv2/internal/results"
	"stockcentraluploadlistproductsv2/internal/workbook"
)

// Este archivo concentra el arranque técnico del modo batch.
// Su responsabilidad es construir el runtime completo del proceso y dejar
// centralizado el logging de inicio y cierre de la corrida.
//
// En otras palabras, este archivo responde una pregunta puntual:
// "¿qué objetos concretos hay que crear para que el batch pueda correr?"
//
// Tener este wiring junto evita que alguien tenga que saltar entre muchos
// archivos solo para entender cómo nace la aplicación.

// BatchRuntime agrupa el batch ya construido y cómo cerrar sus recursos.
type BatchRuntime struct {
	Processor *batch.Processor
	closeFn   func() error
}

// Close libera los recursos abiertos durante el bootstrap.
func (r BatchRuntime) Close() error {
	if r.closeFn == nil {
		return nil
	}
	return r.closeFn()
}

// BuildBatch arma todas las dependencias concretas del modo batch.
func BuildBatch(cfg appconfig.Config, logs logging.LoggerSet) (BatchRuntime, error) {
	// La conexión a SQL es el punto de partida del runtime.
	sqlServer, err := providers.NewSQLServer(cfg.Database, cfg.Secrets.DBConnectionString)
	if err != nil {
		return BatchRuntime{}, fmt.Errorf("bootstrap sqlserver: %w", err)
	}

	// A partir de SQL armamos el resto de los componentes base.
	// Cada uno cubre una parte concreta del flujo:
	// - traer providers;
	// - descubrir archivos;
	// - leer Excel;
	// - hablar con APIs;
	// - mover archivos;
	// - escribir resultados;
	// - y notificar.
	providerRepo := providers.NewSQLServerRepository(sqlServer, cfg.Database)
	scanner := intake.NewScanner(cfg.Paths.InputRoot)
	excelReader := workbook.NewReader()
	productsClient := products.NewClient(cfg.ProductsAPI, cfg.Secrets.ProductsAPIToken)
	categoryResolver := catalog.NewResolver(productsClient)
	imageDownloader := images.NewDownloader(60 * time.Second)
	mover := intake.NewMover(cfg.Paths.ProcessingRoot, cfg.Paths.ProcessedRoot)
	sendGridClient := notifications.NewSendGridClient(cfg.Secrets.SendGridAPIKey)
	notificationService := notifications.NewService(cfg.Notifications, sendGridClient, logs)
	resultsWriter := results.NewWriter()

	// FileProcessor concentra el trabajo completo de un archivo individual.
	fileProcessor := batch.NewFileProcessor(
		cfg.Batch.RowWorkers,
		time.Duration(cfg.Batch.RowTimeoutSeconds)*time.Second,
		cfg.Batch.SyncImages,
		categoryResolver,
		excelReader,
		imageDownloader,
		productsClient,
		mover,
		notificationService,
		resultsWriter,
		logs,
	)

	// Processor se ocupa de la corrida completa: providers -> archivos -> métricas.
	processor := batch.NewProcessor(cfg.Batch, providerRepo, scanner, fileProcessor, logs)

	// Devolvemos el runtime listo para correr junto con su función de cierre.
	return BatchRuntime{
		Processor: processor,
		closeFn: func() error {
			return sqlServer.Close()
		},
	}, nil
}

// LogBatchBootstrap deja registrado el contexto principal de la corrida.
func LogBatchBootstrap(logs logging.LoggerSet, cfg appconfig.Config) {
	// Estos logs ayudan a reconstruir con qué configuración arrancó la corrida.
	logs.Detail.Blank()
	logs.Detail.Info("================================================== BATCH START ==================================================")
	logs.Detail.Debug("batch-context",
		logging.String("app", cfg.App.Name),
		logging.String("environment", cfg.App.Environment),
		logging.String("input_root", cfg.Paths.InputRoot),
		logging.String("processing_root", cfg.Paths.ProcessingRoot),
		logging.String("processed_root", cfg.Paths.ProcessedRoot),
		logging.String("products_api_base_url", cfg.ProductsAPI.BaseURL),
	)
	logs.Summary.Debug("effective-config",
		logging.String("environment", cfg.App.Environment),
		logging.String("products_api_base_url", cfg.ProductsAPI.BaseURL),
		logging.Int("catalog_id", cfg.Batch.CatalogID),
		logging.Int("provider_integrator_id", cfg.Batch.ProviderIntegratorID),
		logging.Int("row_workers", cfg.Batch.RowWorkers),
		logging.Int("row_timeout_seconds", cfg.Batch.RowTimeoutSeconds),
		logging.String("sync_images", fmt.Sprintf("%t", cfg.Batch.SyncImages)),
		logging.String("input_root", cfg.Paths.InputRoot),
	)

	logs.Summary.Info("batch-bootstrap", logging.String("app", cfg.App.Name))
	logs.Summary.Info("config-loaded", logging.String("environment", cfg.App.Environment))
	logs.Summary.Info("paths-ready",
		logging.String("input_root", cfg.Paths.InputRoot),
		logging.String("processing_root", cfg.Paths.ProcessingRoot),
		logging.String("processed_root", cfg.Paths.ProcessedRoot),
	)
	logs.Summary.Info("database-ready",
		logging.String("providers_sp_name", cfg.Database.ProvidersSPName),
		logging.Int("timeout_seconds", cfg.Database.TimeoutSeconds),
	)
	logs.Summary.Info("products-api-ready",
		logging.String("base_url", cfg.ProductsAPI.BaseURL),
		logging.Int("timeout_seconds", cfg.ProductsAPI.TimeoutSeconds),
	)
	logs.Summary.Info("batch-settings",
		logging.Int("catalog_id", cfg.Batch.CatalogID),
		logging.Int("provider_integrator_id", cfg.Batch.ProviderIntegratorID),
		logging.Int("row_workers", cfg.Batch.RowWorkers),
		logging.Int("row_timeout_seconds", cfg.Batch.RowTimeoutSeconds),
	)
}

// LogBatchFinished escribe el cierre resumido y técnico de la corrida.
func LogBatchFinished(logs logging.LoggerSet, result reporting.BatchResult) {
	// Summary deja el panorama operativo; Detail agrega timestamps y duración.
	logs.Summary.Info("batch-finished",
		logging.Int("providers_seen", result.ProvidersSeen),
		logging.Int("files_detected", result.FilesDetected),
		logging.Int("files_processed", result.FilesProcessed),
		logging.Int("files_failed", result.FilesFailed),
	)
	logs.Detail.Info("batch-finished",
		logging.String("started_at", result.StartedAt.Format("2006-01-02 15:04:05")),
		logging.String("finished_at", result.FinishedAt.Format("2006-01-02 15:04:05")),
		logging.String("duration", fmt.Sprintf("%s", result.FinishedAt.Sub(result.StartedAt))),
	)
	logs.Detail.Info("=================================================== BATCH END ===================================================")
	logs.Detail.Blank()
}
