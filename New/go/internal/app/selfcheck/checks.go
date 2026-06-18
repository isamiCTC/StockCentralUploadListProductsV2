package selfcheck

import (
	"fmt"
	"io"
	"os"
	"path/filepath"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/providers"
)

// Este archivo implementa el modo `--self-check`.
// Su responsabilidad es validar que configuración, carpetas, logging y SQL
// estén listos antes de intentar correr el batch real.
//
// La idea es fallar antes, no a mitad del procesamiento.
// Por eso este modo no toca Excels ni hace trabajo de negocio.

// SelfCheckResult representa un chequeo individual del modo self-check.
type SelfCheckResult struct {
	Name   string
	OK     bool
	Detail string
}

// Run valida el ambiente sin ejecutar el batch real.
func Run(settingsPath, envPath string, out io.Writer) error {
	// Primero chequeamos archivos base antes de intentar cargar configuración.
	results := []SelfCheckResult{
		runCheck("settings-file", func() error {
			return ensureFileReadable(settingsPath)
		}),
		runCheck("env-file", func() error {
			return ensureOptionalFileReadable(envPath)
		}),
	}

	// Segundo: intentar cargar la config completa.
	// Si esto falla, no tiene sentido seguir con carpetas o SQL.
	cfg, err := appconfig.Load(settingsPath, envPath)
	if err != nil {
		results = append(results, SelfCheckResult{
			Name:   "config-load",
			OK:     false,
			Detail: err.Error(),
		})
		printResults(out, results)
		return fmt.Errorf("self-check failed")
	}

	// Tercero: con config válida ya podemos revisar filesystem, logs y SQL.
	results = append(results, SelfCheckResult{Name: "config-load", OK: true, Detail: "configuration loaded"})
	results = append(results, collectChecks(cfg)...)

	// Cuarto: imprimir todo junto deja un reporte claro y ordenado.
	printResults(out, results)

	// Quinto: si un solo check falla, el comando debe devolver error.
	for _, result := range results {
		if !result.OK {
			return fmt.Errorf("self-check failed")
		}
	}

	return nil
}

// collectChecks agrupa los chequeos que dependen de una config válida.
func collectChecks(cfg appconfig.Config) []SelfCheckResult {
	// Este bloque reúne validaciones de filesystem, logging y base de datos.
	return []SelfCheckResult{
		runCheck("logging-config", func() error {
			return validateLoggingConfig(cfg.Logging)
		}),
		runCheck("input-root-readable", func() error {
			return ensureDirectoryReadable(cfg.Paths.InputRoot)
		}),
		runCheck("processing-root-writable", func() error {
			return ensureDirectoryWritable(cfg.Paths.ProcessingRoot)
		}),
		runCheck("processed-root-writable", func() error {
			return ensureDirectoryWritable(cfg.Paths.ProcessedRoot)
		}),
		runCheck("log-directory-writable", func() error {
			return ensureDirectoryWritable(cfg.Logging.Directory)
		}),
		runCheck("sqlserver-connection", func() error {
			return ensureSQLServerConnection(cfg)
		}),
	}
}

// runCheck ejecuta una validación y la convierte a un resultado uniforme.
func runCheck(name string, fn func() error) SelfCheckResult {
	// La función concreta decide si el check pasa o falla.
	if err := fn(); err != nil {
		return SelfCheckResult{
			Name:   name,
			OK:     false,
			Detail: err.Error(),
		}
	}

	return SelfCheckResult{
		Name:   name,
		OK:     true,
		Detail: "ok",
	}
}

// printResults escribe el reporte final de checks.
func printResults(out io.Writer, results []SelfCheckResult) {
	for _, result := range results {
		// Traducimos el booleano a una etiqueta simple de consola.
		status := "OK"
		if !result.OK {
			status = "FAIL"
		}

		// La salida es deliberadamente simple para que se lea bien en consola
		// o se pueda copiar tal cual a un ticket o chat.
		fmt.Fprintf(out, "[%s] %s", status, result.Name)
		if result.Detail != "" {
			fmt.Fprintf(out, " - %s", result.Detail)
		}
		fmt.Fprintln(out)
	}
}

// ensureFileReadable confirma que la ruta exista, sea archivo y pueda abrirse.
func ensureFileReadable(path string) error {
	info, err := os.Stat(path)
	if err != nil {
		return fmt.Errorf("read %s: %w", path, err)
	}
	if info.IsDir() {
		return fmt.Errorf("%s is a directory, expected file", path)
	}

	file, err := os.Open(path)
	if err != nil {
		return fmt.Errorf("open %s: %w", path, err)
	}
	defer file.Close()

	return nil
}

// ensureOptionalFileReadable revisa el `.env` con un mensaje más claro.
func ensureOptionalFileReadable(path string) error {
	if path == "" {
		return fmt.Errorf("path is empty")
	}

	if _, err := os.Stat(path); err != nil {
		if os.IsNotExist(err) {
			return fmt.Errorf("%s not found; app can still use environment variables, but this file is missing", path)
		}
		return fmt.Errorf("read %s: %w", path, err)
	}

	return ensureFileReadable(path)
}

// ensureDirectoryReadable confirma que una carpeta exista y se pueda listar.
func ensureDirectoryReadable(path string) error {
	info, err := os.Stat(path)
	if err != nil {
		return fmt.Errorf("stat %s: %w", path, err)
	}
	if !info.IsDir() {
		return fmt.Errorf("%s is not a directory", path)
	}

	if _, err := os.ReadDir(path); err != nil {
		return fmt.Errorf("list %s: %w", path, err)
	}

	return nil
}

// ensureDirectoryWritable confirma que una carpeta permita escritura real.
func ensureDirectoryWritable(path string) error {
	// Esta validación intenta reproducir una escritura real de la app.
	if path == "" {
		return fmt.Errorf("path is empty")
	}

	// Paso 1. Crear la carpeta si todavía no existe.
	if err := os.MkdirAll(path, 0o755); err != nil {
		return fmt.Errorf("create directory %s: %w", path, err)
	}

	// Paso 2. Crear un archivo temporal de prueba.
	testFile, err := os.CreateTemp(path, ".self-check-*")
	if err != nil {
		return fmt.Errorf("create temp file in %s: %w", path, err)
	}

	testName := testFile.Name()
	// Paso 3. Escribir contenido real, para no quedarnos solo con el create.
	if _, err := testFile.WriteString("self-check"); err != nil {
		_ = testFile.Close()
		_ = os.Remove(testName)
		return fmt.Errorf("write temp file in %s: %w", path, err)
	}

	// Paso 4. Cerrar correctamente el archivo.
	if err := testFile.Close(); err != nil {
		_ = os.Remove(testName)
		return fmt.Errorf("close temp file in %s: %w", path, err)
	}

	// Paso 5. Borrarlo para no ensuciar la carpeta.
	if err := os.Remove(testName); err != nil {
		return fmt.Errorf("cleanup temp file in %s: %w", path, err)
	}

	return nil
}

// validateLoggingConfig revisa que el bloque de logging tenga datos mínimos.
func validateLoggingConfig(cfg appconfig.LoggingConfig) error {
	if cfg.Directory == "" {
		return fmt.Errorf("logging.directory is empty")
	}
	if cfg.Summary.Filename == "" {
		return fmt.Errorf("logging.summary.filename is empty")
	}
	if cfg.Detail.Filename == "" {
		return fmt.Errorf("logging.detail.filename is empty")
	}
	if filepath.Base(cfg.Summary.Filename) != cfg.Summary.Filename {
		return fmt.Errorf("logging.summary.filename must be a file name, not a nested path")
	}
	if filepath.Base(cfg.Detail.Filename) != cfg.Detail.Filename {
		return fmt.Errorf("logging.detail.filename must be a file name, not a nested path")
	}

	return nil
}

// ensureSQLServerConnection intenta abrir la conexión real a SQL Server.
func ensureSQLServerConnection(cfg appconfig.Config) error {
	server, err := providers.NewSQLServer(cfg.Database, cfg.Secrets.DBConnectionString)
	if err != nil {
		return err
	}
	defer server.Close()

	return nil
}
