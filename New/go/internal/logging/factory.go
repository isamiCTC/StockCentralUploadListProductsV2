package logging

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"

	appconfig "stockcentraluploadlistproductsv2/internal/config"

	"gopkg.in/natefinch/lumberjack.v2"
)

// Este archivo arma las instancias concretas del logger usando la
// configuración de la aplicación.
//
// Acá se decide qué writer usa cada log, dónde viven los archivos y cómo
// aplicar rotación/retención con lumberjack.

// MustNew es el constructor "duro" usado durante el arranque.
// Si el logger no puede inicializarse, se considera un error fatal.
func MustNew(cfg appconfig.LoggingConfig) LoggerSet {
	logs, err := NewLoggerSet(cfg)
	if err != nil {
		panic(err)
	}

	return logs
}

// NewLoggerSet crea el log summary y el log detail.
// Summary escribe a consola + archivo.
// Detail escribe solo a archivo.
func NewLoggerSet(cfg appconfig.LoggingConfig) (LoggerSet, error) {
	// Preparamos la carpeta una sola vez al inicio para que después los writes
	// no fallen por un problema de estructura.
	if err := os.MkdirAll(cfg.Directory, 0o755); err != nil {
		return LoggerSet{}, fmt.Errorf("create log directory: %w", err)
	}

	// Summary replica a stdout porque es el log que queremos ver en ejecución.
	summaryWriter := io.MultiWriter(
		os.Stdout,
		newRollingFile(filepath.Join(cfg.Directory, cfg.Summary.Filename), cfg.Summary),
	)

	// Detail queda solo en archivo porque puede crecer bastante más.
	detailWriter := newRollingFile(filepath.Join(cfg.Directory, cfg.Detail.Filename), cfg.Detail)

	return LoggerSet{
		Summary: New(parseLevel(cfg.ConsoleLevel), summaryWriter),
		Detail:  New(parseLevel(cfg.Level), detailWriter),
	}, nil
}

// newRollingFile arma el writer rotativo sobre el que escriben los logs.
func newRollingFile(path string, cfg appconfig.LogFileConfig) io.Writer {
	return &lumberjack.Logger{
		// Lumberjack se ocupa de cortar y conservar archivos viejos sin que el
		// resto del código tenga que pensar en eso.
		Filename:   path,
		MaxSize:    cfg.MaxSizeMB,
		MaxBackups: cfg.MaxBackups,
		MaxAge:     cfg.MaxAgeDays,
		Compress:   false,
	}
}

// parseLevel traduce el texto configurado en TOML al enum interno.
func parseLevel(level string) Level {
	// Limpiamos el texto para tolerar diferencias menores de formato en config.
	switch strings.ToUpper(strings.TrimSpace(level)) {
	case "DEBUG":
		return LevelDebug
	case "WARN", "WARNING":
		return LevelWarn
	case "ERROR":
		return LevelError
	default:
		return LevelInfo
	}
}
