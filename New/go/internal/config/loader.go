package config

import (
	"fmt"
	"os"

	"github.com/BurntSushi/toml"
	"github.com/joho/godotenv"
)

// Este archivo se encarga de leer `appsettings.toml` y `.env`, unir ambos
// orígenes en una sola estructura y validar que la aplicación tenga lo mínimo
// necesario para arrancar.
//
// La idea es fallar temprano: si falta un dato crítico, el proceso debe cortar
// al inicio y no a mitad del batch.
type fileConfig struct {
	App           AppConfig
	Batch         BatchConfig       `toml:"batch"`
	Paths         PathsConfig       `toml:"paths"`
	Database      DatabaseConfig    `toml:"database"`
	ProductsAPI   ProductsAPIConfig `toml:"products_api"`
	Logging       LoggingConfig     `toml:"logging"`
	Notifications NotificationsConfig
}

// MustLoad es el helper de arranque rápido usado por `main`.
// Si la configuración no es válida, hace panic para cortar el proceso
// en el bootstrap.
func MustLoad(settingsPath, envPath string) Config {
	cfg, err := Load(settingsPath, envPath)
	if err != nil {
		panic(err)
	}

	return cfg
}

// Load hace el trabajo real:
// 1. lee TOML
// 2. intenta leer `.env`
// 3. arma la struct final
// 4. valida los campos obligatorios
func Load(settingsPath, envPath string) (Config, error) {
	var fc fileConfig
	// El TOML contiene toda la configuración "visible" y versionable.
	if _, err := toml.DecodeFile(settingsPath, &fc); err != nil {
		return Config{}, fmt.Errorf("decode appsettings.toml: %w", err)
	}

	// Si `.env` no existe, godotenv devuelve error, pero por ahora no lo
	// tratamos como fatal porque las variables podrían venir del entorno.
	_ = godotenv.Load(envPath)

	// Unimos config de archivo con secretos de variables de entorno.
	cfg := Config{
		App:           fc.App,
		Batch:         fc.Batch,
		Paths:         fc.Paths,
		Database:      fc.Database,
		ProductsAPI:   fc.ProductsAPI,
		Logging:       fc.Logging,
		Notifications: fc.Notifications,
		Secrets: SecretsConfig{
			DBConnectionString: os.Getenv("DB_CONNECTION_STRING"),
			ProductsAPIToken:   os.Getenv("PRODUCTS_API_TOKEN"),
			SendGridAPIKey:     os.Getenv("SENDGRID_API_KEY"),
		},
	}

	// Validamos antes de devolver para cortar problemas en bootstrap.
	if err := validate(cfg); err != nil {
		return Config{}, err
	}

	return cfg, nil
}

// validate revisa únicamente condiciones de arranque.
// No valida todavía reglas de negocio ni detalles de Excel.
func validate(cfg Config) error {
	// Primero validamos identidad mínima y rutas base del proceso.
	if cfg.App.Name == "" {
		return fmt.Errorf("missing app.name")
	}
	if cfg.Paths.InputRoot == "" {
		return fmt.Errorf("missing paths.input_root")
	}
	if cfg.Paths.ProcessingRoot == "" {
		return fmt.Errorf("missing paths.processing_root")
	}
	if cfg.Paths.ProcessedRoot == "" {
		return fmt.Errorf("missing paths.processed_root")
	}
	if cfg.Database.TimeoutSeconds <= 0 {
		return fmt.Errorf("invalid database.timeout_seconds")
	}
	if cfg.Database.ProvidersSPName == "" {
		return fmt.Errorf("missing database.providers_sp_name")
	}
	if cfg.ProductsAPI.BaseURL == "" {
		return fmt.Errorf("missing products_api.base_url")
	}
	// Después validamos secretos mínimos para DB y API de productos.
	if cfg.Secrets.DBConnectionString == "" {
		return fmt.Errorf("missing DB_CONNECTION_STRING in .env")
	}
	if cfg.Secrets.ProductsAPIToken == "" {
		return fmt.Errorf("missing PRODUCTS_API_TOKEN in .env")
	}
	// SendGrid solo es obligatorio si el módulo de notificaciones está activo.
	if cfg.Notifications.Enabled {
		if cfg.Notifications.FromEmail == "" {
			return fmt.Errorf("missing notifications.from_email")
		}
		if cfg.Secrets.SendGridAPIKey == "" {
			return fmt.Errorf("missing SENDGRID_API_KEY in .env")
		}
	}

	return nil
}
