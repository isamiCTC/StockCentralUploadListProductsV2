package main

import (
	"context"
	"fmt"
	"os"
	"time"

	"stockcentraluploadlistproductsv2/internal/batch"
	"stockcentraluploadlistproductsv2/internal/catalog"
	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/excel"
	"stockcentraluploadlistproductsv2/internal/files"
	"stockcentraluploadlistproductsv2/internal/images"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/notifications"
	"stockcentraluploadlistproductsv2/internal/products"
	"stockcentraluploadlistproductsv2/internal/providers"
	"stockcentraluploadlistproductsv2/internal/results"
)

// main es el punto de entrada del batch.
//
// Este archivo no contiene lógica de negocio ni helpers: solo arma las
// dependencias principales, las conecta entre sí y dispara la corrida.
// La idea es que alguien nuevo pueda leer este archivo de arriba hacia abajo
// y entender el flujo general completo.
func main() {
	// Creamos un contexto base para toda la corrida del proceso.
	ctx := context.Background()

	// Cargamos primero la configuración para saber cómo inicializar el resto.
	cfg := appconfig.MustLoad("config/appsettings.toml", "config/.env")

	// Levantamos el sistema de logging lo antes posible para poder registrar
	// cualquier error de bootstrap que ocurra después.
	logs := logging.MustNew(cfg.Logging)

	// Abrimos la conexión a SQL Server porque el primer paso real del batch
	// es consultar los providers válidos desde base de datos.
	sqlServer, err := providers.NewSQLServer(cfg.Database, cfg.Secrets.DBConnectionString)
	if err != nil {
		logs.Summary.Error("sqlserver-bootstrap-failed", logging.String("error", err.Error()))
		logs.Detail.Error("sqlserver-bootstrap-failed", logging.String("error", err.Error()))
		os.Exit(1)
	}

	// Cerramos la conexión al terminar la corrida, incluso si luego falla algo.
	defer func() {
		if closeErr := sqlServer.Close(); closeErr != nil {
			logs.Detail.Warn("sqlserver-close-failed", logging.String("error", closeErr.Error()))
		}
	}()

	// A partir de la conexión armamos el repositorio concreto que ejecuta
	// el stored procedure legado de providers.
	providerRepo := providers.NewSQLServerRepository(sqlServer, cfg.Database)

	// Scanner se encarga de encontrar archivos válidos dentro de las carpetas
	// de providers habilitados.
	scanner := files.NewScanner(cfg.Paths.InputRoot)

	// Reader concentra la lectura y validación estructural de los `.xlsx`.
	excelReader := excel.NewReader()

	// Client encapsula la API REST de productos respetando la semántica legacy.
	productsClient := products.NewClient(cfg.ProductsAPI, cfg.Secrets.ProductsAPIToken)

	// Resolver aplica el mapeo hardcodeado de categorías y su fallback a API.
	categoryResolver := catalog.NewResolver(productsClient)

	// Downloader resuelve la descarga y serialización Base64 de imágenes remotas.
	imageDownloader := images.NewDownloader(60 * time.Second)

	// Mover centraliza la lógica de armar rutas y mover archivos entre
	// input, processing y processed.
	mover := files.NewMover(cfg.Paths.ProcessingRoot, cfg.Paths.ProcessedRoot)

	// Notification service decide destinatarios, asunto y adjunto, y delega
	// el envío real a SendGrid.
	sendGridClient := notifications.NewSendGridClient(cfg.Secrets.SendGridAPIKey)
	notificationService := notifications.NewService(cfg.Notifications, sendGridClient, logs)

	// Writer genera los excels auxiliares que luego servirán para auditoría
	// humana y también para las notificaciones por mail del cierre.
	resultsWriter := results.NewWriter()

	// FileProcessor procesa un archivo por vez: Excel, mapping, worker pool
	// por fila, categoría, API de productos y archivo final de resultados.
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

	// Processor es el orquestador principal del batch: trae providers,
	// descubre archivos y delega el procesamiento archivo por archivo.
	processor := batch.NewProcessor(cfg.Batch, providerRepo, scanner, fileProcessor, logs)

	// Registramos en summary la configuración operativa principal para que
	// cada corrida quede auditable desde el inicio.
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

	// Ejecutamos la corrida completa del batch.
	result, err := processor.Run(ctx)
	if err != nil {
		logs.Summary.Error("batch-failed", logging.String("error", err.Error()))
		logs.Detail.Error("batch-failed", logging.String("error", err.Error()))
		os.Exit(1)
	}

	// Si todo terminó, dejamos un cierre resumido en summary y otro más técnico
	// en detail con timestamps y duración.
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
}
