package config

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestLoadReadsSecretsFromEnvFile(t *testing.T) {
	restore := clearEnvForLoad(t)
	defer restore()

	tempDir := t.TempDir()
	settingsPath := filepath.Join(tempDir, "appsettings.toml")
	envPath := filepath.Join(tempDir, ".env")

	writeTestFile(t, settingsPath, validTestSettingsTOML(true))
	writeTestFile(t, envPath, strings.Join([]string{
		"DB_CONNECTION_STRING=sqlserver://user:password@host:1433?database=StockCentral",
		"PRODUCTS_API_TOKEN=test-products-token",
		"SENDGRID_API_KEY=test-sendgrid-key",
	}, "\n"))

	cfg, err := Load(settingsPath, envPath)
	if err != nil {
		t.Fatalf("Load returned error: %v", err)
	}
	if cfg.Secrets.DBConnectionString != "sqlserver://user:password@host:1433?database=StockCentral" {
		t.Fatalf("DBConnectionString = %q", cfg.Secrets.DBConnectionString)
	}
	if cfg.Secrets.ProductsAPIToken != "test-products-token" {
		t.Fatalf("ProductsAPIToken = %q", cfg.Secrets.ProductsAPIToken)
	}
	if cfg.Secrets.SendGridAPIKey != "test-sendgrid-key" {
		t.Fatalf("SendGridAPIKey = %q", cfg.Secrets.SendGridAPIKey)
	}
}

func TestLoadFailsWhenProductsTokenIsMissing(t *testing.T) {
	restore := clearEnvForLoad(t)
	defer restore()

	tempDir := t.TempDir()
	settingsPath := filepath.Join(tempDir, "appsettings.toml")
	envPath := filepath.Join(tempDir, ".env")

	writeTestFile(t, settingsPath, validTestSettingsTOML(false))
	writeTestFile(t, envPath, "DB_CONNECTION_STRING=sqlserver://user:password@host:1433?database=StockCentral\n")

	_, err := Load(settingsPath, envPath)
	if err == nil {
		t.Fatal("Load should fail when PRODUCTS_API_TOKEN is missing")
	}
	if !strings.Contains(err.Error(), "missing PRODUCTS_API_TOKEN in .env") {
		t.Fatalf("Load error = %q, want missing PRODUCTS_API_TOKEN message", err.Error())
	}
}

func TestLoadFailsWhenNotificationsAreEnabledButSendGridKeyIsMissing(t *testing.T) {
	restore := clearEnvForLoad(t)
	defer restore()

	tempDir := t.TempDir()
	settingsPath := filepath.Join(tempDir, "appsettings.toml")
	envPath := filepath.Join(tempDir, ".env")

	writeTestFile(t, settingsPath, validTestSettingsTOML(true))
	writeTestFile(t, envPath, strings.Join([]string{
		"DB_CONNECTION_STRING=sqlserver://user:password@host:1433?database=StockCentral",
		"PRODUCTS_API_TOKEN=test-products-token",
	}, "\n"))

	_, err := Load(settingsPath, envPath)
	if err == nil {
		t.Fatal("Load should fail when SENDGRID_API_KEY is missing and notifications are enabled")
	}
	if !strings.Contains(err.Error(), "missing SENDGRID_API_KEY in .env") {
		t.Fatalf("Load error = %q, want missing SENDGRID_API_KEY message", err.Error())
	}
}

func writeTestFile(t *testing.T, path, content string) {
	t.Helper()

	if err := os.WriteFile(path, []byte(content), 0o600); err != nil {
		t.Fatalf("write %s: %v", path, err)
	}
}

func clearEnvForLoad(t *testing.T) func() {
	t.Helper()

	keys := []string{"DB_CONNECTION_STRING", "PRODUCTS_API_TOKEN", "SENDGRID_API_KEY"}
	previous := make(map[string]*string, len(keys))
	for _, key := range keys {
		value, ok := os.LookupEnv(key)
		if ok {
			copyValue := value
			previous[key] = &copyValue
		}
		if err := os.Unsetenv(key); err != nil {
			t.Fatalf("Unsetenv(%s): %v", key, err)
		}
	}

	return func() {
		for _, key := range keys {
			if previous[key] == nil {
				_ = os.Unsetenv(key)
				continue
			}
			_ = os.Setenv(key, *previous[key])
		}
	}
}

func validTestSettingsTOML(notificationsEnabled bool) string {
	enabled := "false"
	if notificationsEnabled {
		enabled = "true"
	}

	return `
[app]
name = "StockCentralUploadListProductsV2"
environment = "test"

[batch]
catalog_id = 31
provider_integrator_id = 3
sync_images = true
stop_on_file_error = false
row_workers = 2
row_timeout_seconds = 30

[paths]
input_root = "C:/input"
processing_root = "C:/processing"
processed_root = "C:/processed"

[database]
timeout_seconds = 30
providers_sp_name = "ProvidersGetListByEnabledAndIntegratorAndCatalogID"

[products_api]
base_url = "http://example.test"
provider_name = "CTC"
timeout_seconds = 30

[notifications]
enabled = ` + enabled + `
from_email = "alerts@example.test"
always_recipients = ["ops@example.test"]

[logging]
directory = "./logs"
level = "debug"
console_level = "info"

[logging.summary]
filename = "summary.log"
max_size_mb = 10
max_backups = 3
max_age_days = 7

[logging.detail]
filename = "detail.log"
max_size_mb = 10
max_backups = 3
max_age_days = 7
`
}
