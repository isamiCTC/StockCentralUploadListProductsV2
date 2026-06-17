package selfcheck

import (
	"fmt"
	"io"
	"os"
	"path/filepath"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/providers"
)

// Este archivo reúne chequeos simples para confirmar que el ambiente está
// listo antes de correr el batch de verdad.
//
// La idea no es procesar nada, sino responder preguntas concretas como:
// - ¿la configuración carga?
// - ¿las carpetas existen y se pueden usar?
// - ¿hay permiso para escribir logs?
// - ¿SQL Server responde?

// Result guarda el resultado de un chequeo puntual.
//
// Cada item representa una sola validación para que, si algo falla, el motivo
// se vea rápido y no quede escondido dentro de un error más grande.
type Result struct {
	Name   string
	OK     bool
	Detail string
}

// Run corre el modo self-check completo sin arrancar el batch.
//
// El flujo es:
// 1. revisar que los archivos de config existan
// 2. intentar cargar la configuración real
// 3. validar carpetas, permisos y conexión a SQL
// 4. imprimir un resumen entendible
func Run(settingsPath, envPath string, out io.Writer) error {
	// Primero verificamos que los archivos base estén donde esperamos.
	results := []Result{
		runCheck("settings-file", func() error {
			return ensureFileReadable(settingsPath)
		}),
		runCheck("env-file", func() error {
			return ensureOptionalFileReadable(envPath)
		}),
	}

	// Si la configuración no carga, frenamos acá porque el resto de los
	// chequeos depende de esos datos.
	cfg, err := appconfig.Load(settingsPath, envPath)
	if err != nil {
		results = append(results, Result{
			Name:   "config-load",
			OK:     false,
			Detail: err.Error(),
		})
		printResults(out, results)
		return fmt.Errorf("self-check failed")
	}

	// Si cargó bien, seguimos con los chequeos que ya usan la config real.
	results = append(results, Result{Name: "config-load", OK: true, Detail: "configuration loaded"})
	results = append(results, collectChecks(cfg)...)

	// Siempre mostramos el detalle completo para que sea fácil ver qué pasó,
	// incluso cuando uno de los pasos falla.
	printResults(out, results)

	// Si al menos un check falló, devolvemos error para que el proceso termine
	// con código distinto de cero.
	for _, result := range results {
		if !result.OK {
			return fmt.Errorf("self-check failed")
		}
	}

	return nil
}

// collectChecks junta los chequeos que dependen de una config ya cargada.
// Así la parte principal de Run queda más fácil de seguir.
func collectChecks(cfg appconfig.Config) []Result {
	return []Result{
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

// runCheck ejecuta una validación y la convierte en un resultado uniforme.
// Esto evita repetir siempre el mismo manejo de OK/FAIL.
func runCheck(name string, fn func() error) Result {
	if err := fn(); err != nil {
		return Result{
			Name:   name,
			OK:     false,
			Detail: err.Error(),
		}
	}

	return Result{
		Name:   name,
		OK:     true,
		Detail: "ok",
	}
}

// printResults escribe la salida final línea por línea.
// La intención es que se pueda leer rápido en consola o en un log.
func printResults(out io.Writer, results []Result) {
	for _, result := range results {
		status := "OK"
		if !result.OK {
			status = "FAIL"
		}

		fmt.Fprintf(out, "[%s] %s", status, result.Name)
		if result.Detail != "" {
			fmt.Fprintf(out, " - %s", result.Detail)
		}
		fmt.Fprintln(out)
	}
}

// ensureFileReadable confirma que la ruta exista, sea archivo y se pueda abrir.
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

// ensureOptionalFileReadable trata al `.env` como archivo esperado, pero deja
// un mensaje más claro si no está porque la app también podría leer variables
// del entorno del sistema.
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

// ensureDirectoryReadable confirma que una carpeta exista y que al menos se
// pueda listar su contenido.
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

// ensureDirectoryWritable prueba escritura real creando un archivo temporal.
// No alcanza con que la carpeta exista: queremos comprobar que el proceso
// realmente puede dejar archivos ahí.
func ensureDirectoryWritable(path string) error {
	if path == "" {
		return fmt.Errorf("path is empty")
	}

	// Si la carpeta no existe todavía, intentamos crearla igual que haría la app.
	if err := os.MkdirAll(path, 0o755); err != nil {
		return fmt.Errorf("create directory %s: %w", path, err)
	}

	// El archivo temporal nos sirve para validar creación, escritura y cierre.
	testFile, err := os.CreateTemp(path, ".self-check-*")
	if err != nil {
		return fmt.Errorf("create temp file in %s: %w", path, err)
	}

	testName := testFile.Name()
	// Escribimos un contenido mínimo para confirmar permiso real de escritura.
	if _, err := testFile.WriteString("self-check"); err != nil {
		_ = testFile.Close()
		_ = os.Remove(testName)
		return fmt.Errorf("write temp file in %s: %w", path, err)
	}

	if err := testFile.Close(); err != nil {
		_ = os.Remove(testName)
		return fmt.Errorf("close temp file in %s: %w", path, err)
	}

	// Al final limpiamos el archivo para no dejar basura en el ambiente.
	if err := os.Remove(testName); err != nil {
		return fmt.Errorf("cleanup temp file in %s: %w", path, err)
	}

	return nil
}

// validateLoggingConfig revisa que el bloque de logging tenga lo mínimo
// necesario para poder armar los archivos de salida.
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

// ensureSQLServerConnection intenta abrir y validar la conexión real.
// Si esto falla, el batch tampoco podría arrancar bien después.
func ensureSQLServerConnection(cfg appconfig.Config) error {
	server, err := providers.NewSQLServer(cfg.Database, cfg.Secrets.DBConnectionString)
	if err != nil {
		return err
	}
	defer server.Close()

	return nil
}
