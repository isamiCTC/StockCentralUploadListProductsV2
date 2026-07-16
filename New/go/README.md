# StockCentralUploadListProductsV2 (Go)

Reescritura en Go del proceso legacy de carga masiva de productos por Excel.

Este proyecto implementa un **batch de una sola corrida** (one-shot):

- detecta archivos `.xlsx` de providers válidos;
- valida estructura del archivo;
- procesa filas de forma concurrente;
- impacta productos (y opcionalmente imágenes) vía API;
- genera archivo de resultados;
- envía notificación por correo con adjuntos.

## Estado y enfoque

La V2 mantiene la lógica funcional principal del legado, pero cambia el modelo operativo:

- no es un Windows Service en loop;
- no hace scheduling interno;
- termina al finalizar la corrida;
- deja la planificación a un scheduler externo (Task Scheduler, cron, etc.).

## Estructura principal

```text
New/go/
  cmd/StockCentralUploadListProductsV2/   # entrypoint del binario
  internal/                               # lógica de aplicación y dominio
  config/                                 # appsettings.example.toml y .env.example
  docs/                                   # documentación funcional/técnica
  sql/                                    # scripts SQL versionados
  scripts/                                # build.ps1 y test.ps1
```

## Requisitos

- Go `1.26`
- Acceso a SQL Server
- Acceso a Products API
- API key de SendGrid (si notificaciones están habilitadas)

## Configuración inicial

1. Crear archivos reales de configuración a partir de los ejemplos:

```powershell
Copy-Item config/appsettings.example.toml config/appsettings.toml
Copy-Item config/.env.example config/.env
```

2. Completar valores reales en:

- `config/appsettings.toml`
- `config/.env`

Campos clave esperados:

- SQL Server: `DB_CONNECTION_STRING`
- API de productos: `PRODUCTS_API_TOKEN`
- Email: `SENDGRID_API_KEY`
- Paths batch: `paths.input_root`, `paths.processing_root`, `paths.processed_root`

La API de productos incluye una política configurable de recuperación ante
interbloqueos transitorios de SQL Server:

```toml
[products_api]
deadlock_max_attempts = 3
deadlock_base_delay_ms = 500
```

La política aplica al `PUT` y al `POST` de productos, y al `PUT` de stock. Solo
reintenta cuando `Result.Description` contiene el mensaje específico de
interbloqueo que pide ejecutar nuevamente la transacción; otros errores HTTP no
se reintentan.

## Ejecución

### Self-check (recomendado antes de run)

```powershell
go run ./cmd/StockCentralUploadListProductsV2 self-check --settings config/appsettings.toml --env config/.env
```

Valida configuración, acceso a filesystem y conectividad SQL.

### Correr batch

```powershell
go run ./cmd/StockCentralUploadListProductsV2 run --settings config/appsettings.toml --env config/.env
```

## Scripts auxiliares (PowerShell)

### Build

```powershell
./scripts/build.ps1
```

Genera artefacto en `dist/<os>-<arch>/`, copia `config/` y usa `upx` si está disponible.

### Tests

```powershell
./scripts/test.ps1
```

Ejecuta `go test -count=1 ./...` y muestra resumen final.

## Documentación funcional y operativa

La carpeta `docs/` es la fuente de verdad funcional para V2:

- `docs/PROCESO_FUNCIONAL_CARGA_PRODUCTOS.md`
  - proceso de punta a punta orientado a negocio y operación.
- `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md`
  - detalle técnico profundo del comportamiento implementado.
- `docs/GUIA_CARGA_ARCHIVO_SELLERS.md`
  - guía para sellers sobre uso de plantillas (formato completo/corto).
- `docs/TESTS_UNITARIOS.md`
  - inventario de cobertura de tests unitarios y su objetivo.
- `docs/SQL.md`
  - índice de scripts SQL nuevos versionados.

Archivos de ejemplo incluidos:

- `docs/EJEMPLO_ARCHIVO_COMPLETO.xlsx`
- `docs/EJEMPLO_ARCHIVO_STOCK.xlsx`

## SQL versionado

Scripts nuevos del proceso viven en `sql/`.

Actualmente:

- `sql/CatalogCategoryBranchLookup_Get.sql`
- `sql/ufnNormalizeCatalogCategoryName.sql`

Detalle de SQL en:

- `docs/SQL.md`

## Comandos útiles de desarrollo

```powershell
go test ./...
go test ./internal/workbook/...
go test ./internal/integrations/productsapi/...
go test ./internal/integrations/sqlserver/...
go build ./cmd/StockCentralUploadListProductsV2
```

## Nota de operación

Este proceso está diseñado para ejecución batch no interactiva y alto volumen por archivo.

Si se requiere ejecución periódica, debe resolverse externamente con un scheduler.
