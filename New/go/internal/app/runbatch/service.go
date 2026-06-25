package runbatch

import (
	"context"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/logging"
)

// Este archivo define el "caso de uso" de correr el batch completo.
//
// La idea de esta capa es simple:
// - recibir parámetros de entrada ya resueltos;
// - preparar config y logging;
// - pedirle a la capa de runtime que arme dependencias concretas;
// - ejecutar el proceso;
// - y devolver un exit code.
//
// Así evitamos que la CLI conozca detalles de SQL, scanner, Excel o mails.
// Execute carga configuración, arma el runtime y corre el batch completo.
func Execute(ctx context.Context, settingsPath, envPath string) int {
	// Paso 1. Leer config y secretos. Si esto falla, el proceso no puede seguir.
	cfg := appconfig.MustLoad(settingsPath, envPath)

	// Paso 2. Levantar logging lo antes posible para poder auditar fallas.
	logs := logging.MustNew(cfg.Logging)

	// Paso 3. Construir todas las dependencias concretas del batch.
	runtime, err := BuildBatch(cfg, logs)
	if err != nil {
		logs.Summary.Error("batch-bootstrap-failed", logging.String("error", err.Error()))
		logs.Detail.Error("batch-bootstrap-failed", logging.String("error", err.Error()))
		return 1
	}

	// Paso 4. Asegurar liberación de recursos, aunque el batch falle.
	defer func() {
		if closeErr := runtime.Close(); closeErr != nil {
			logs.Summary.Warn("sqlserver-close-failed", logging.String("error", closeErr.Error()))
			logs.Detail.Warn("sqlserver-close-failed", logging.String("error", closeErr.Error()))
		}
	}()

	// Paso 5. Registrar con qué parámetros principales arrancamos.
	LogBatchBootstrap(logs, cfg)

	// Paso 6. Ejecutar el proceso principal.
	result, err := runtime.Processor.Run(ctx)
	if err != nil {
		LogBatchAborted(logs, err)
		return 1
	}

	// Paso 7. Registrar el cierre correcto de la corrida.
	LogBatchFinished(logs, result)
	return 0
}
