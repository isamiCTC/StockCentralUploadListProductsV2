package config

// Este archivo define las estructuras de configuración que representan
// exactamente lo que el proceso puede leer desde `appsettings.toml` y `.env`.
//
// La responsabilidad de este archivo es solo modelar datos. No valida, no lee
// archivos y no toma decisiones de runtime.
type Config struct {
	App           AppConfig
	Batch         BatchConfig       `toml:"batch"`
	Paths         PathsConfig       `toml:"paths"`
	Database      DatabaseConfig    `toml:"database"`
	ProductsAPI   ProductsAPIConfig `toml:"products_api"`
	Logging       LoggingConfig     `toml:"logging"`
	Notifications NotificationsConfig
	Secrets       SecretsConfig
}

// AppConfig describe datos generales del binario.
type AppConfig struct {
	Name        string
	Environment string
}

// BatchConfig concentra las decisiones operativas de la corrida batch.
type BatchConfig struct {
	CatalogID            int  `toml:"catalog_id"`
	ProviderIntegratorID int  `toml:"provider_integrator_id"`
	SyncImages           bool `toml:"sync_images"`
	StopOnFileError      bool `toml:"stop_on_file_error"`
	RowWorkers           int  `toml:"row_workers"`
	RowTimeoutSeconds    int  `toml:"row_timeout_seconds"`
}

// PathsConfig agrupa las rutas principales del ciclo de vida del archivo.
type PathsConfig struct {
	InputRoot      string `toml:"input_root"`
	ProcessingRoot string `toml:"processing_root"`
	ProcessedRoot  string `toml:"processed_root"`
}

// DatabaseConfig contiene parámetros no sensibles para hablar con SQL Server.
type DatabaseConfig struct {
	TimeoutSeconds  int    `toml:"timeout_seconds"`
	ProvidersSPName string `toml:"providers_sp_name"`
}

// ProductsAPIConfig define cómo hablar con la API de productos.
type ProductsAPIConfig struct {
	BaseURL        string `toml:"base_url"`
	ProviderName   string `toml:"provider_name"`
	TimeoutSeconds int    `toml:"timeout_seconds"`
}

// LoggingConfig define directorio, nivel y configuración de ambos archivos de log.
type LoggingConfig struct {
	Directory    string        `toml:"directory"`
	Level        string        `toml:"level"`
	ConsoleLevel string        `toml:"console_level"`
	Summary      LogFileConfig `toml:"summary"`
	Detail       LogFileConfig `toml:"detail"`
}

// LogFileConfig modela la política de rotación de un archivo de log.
type LogFileConfig struct {
	Filename   string `toml:"filename"`
	MaxSizeMB  int    `toml:"max_size_mb"`
	MaxBackups int    `toml:"max_backups"`
	MaxAgeDays int    `toml:"max_age_days"`
}

// NotificationsConfig agrupa la configuración de mails salientes.
//
// La regla funcional acordada es:
// - siempre enviar a `AlwaysRecipients`
// - además enviar al email del provider si el SP lo trae
type NotificationsConfig struct {
	Enabled          bool     `toml:"enabled"`
	FromEmail        string   `toml:"from_email"`
	AlwaysRecipients []string `toml:"always_recipients"`
}

// SecretsConfig representa los secretos leídos desde `.env`.
type SecretsConfig struct {
	DBConnectionString string
	ProductsAPIToken   string
	SendGridAPIKey     string
}
