package main

import (
	"context"
	"flag"
	"fmt"
	"os"

	"stockcentraluploadlistproductsv2/internal/bootstrap"
	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/logging"
)

// main es la puerta de entrada del binario.
// Solo decide qué modo correr y delega el trabajo real.
func main() {
	runMode := flag.Bool("run", false, "run the batch process")
	selfCheckMode := flag.Bool("self-check", false, "run environment checks and exit")
	settingsPath := flag.String("settings", "config/appsettings.toml", "path to appsettings.toml")
	envPath := flag.String("env", "config/.env", "path to .env file")
	flag.Parse()

	// No permitimos ejecutar ambos modos al mismo tiempo.
	if *runMode && *selfCheckMode {
		fmt.Fprintln(os.Stdout, "Choose only one mode: --run or --self-check.")
		flag.Usage()
		os.Exit(1)
	}

	if *selfCheckMode {
		os.Exit(runSelfCheck(*settingsPath, *envPath))
	}

	// Exigimos intención explícita para no disparar el batch por accidente.
	if !*runMode {
		fmt.Fprintln(os.Stdout, "No mode selected. Use --run to process files or --self-check to validate the environment.")
		flag.Usage()
		os.Exit(1)
	}

	os.Exit(runBatch(*settingsPath, *envPath))
}

// runSelfCheck valida el ambiente y devuelve código de salida.
func runSelfCheck(settingsPath, envPath string) int {
	if err := bootstrap.RunSelfCheck(settingsPath, envPath, os.Stdout); err != nil {
		return 1
	}

	return 0
}

// runBatch ejecuta el flujo principal y devuelve código de salida.
func runBatch(settingsPath, envPath string) int {
	// Preparación mínima del proceso: contexto, configuración y logging.
	ctx := context.Background()
	cfg := appconfig.MustLoad(settingsPath, envPath)
	logs := logging.MustNew(cfg.Logging)

	// Bootstrap arma todas las dependencias concretas del batch.
	runtime, err := bootstrap.BuildBatch(cfg, logs)
	if err != nil {
		logs.Summary.Error("batch-bootstrap-failed", logging.String("error", err.Error()))
		logs.Detail.Error("batch-bootstrap-failed", logging.String("error", err.Error()))
		return 1
	}

	// Cerramos recursos abiertos por el bootstrap al final de la corrida.
	defer func() {
		if closeErr := runtime.Close(); closeErr != nil {
			logs.Detail.Warn("sqlserver-close-failed", logging.String("error", closeErr.Error()))
		}
	}()

	// Dejamos auditado con qué parámetros arranca la ejecución.
	bootstrap.LogBatchBootstrap(logs, cfg)

	// Ejecutamos el batch real.
	result, err := runtime.Processor.Run(ctx)
	if err != nil {
		logs.Summary.Error("batch-failed", logging.String("error", err.Error()))
		logs.Detail.Error("batch-failed", logging.String("error", err.Error()))
		return 1
	}

	// Si terminó bien, registramos el cierre y sus métricas principales.
	bootstrap.LogBatchFinished(logs, result)
	return 0
}
