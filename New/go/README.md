# Reescritura Go

Esta carpeta contiene el diseño y la futura implementación en Go del reemplazo de `StockCentralUploadListProducts`.

Nombre del nuevo proyecto:

- `StockCentralUploadListProductsV2`

Objetivo de esta etapa:

- capturar el diseño antes de escribir código
- acordar estructura de carpetas, responsabilidades y contratos
- separar claramente la nueva arquitectura del código legacy en .NET

Principios ya acordados:

- el nuevo binario será un batch de una sola pasada
- el scheduling vivirá por fuera del proceso
- la configuración no sensible estará en `config/appsettings.toml`
- los secretos vivirán en `config/.env`
- el `main.go` será un orquestador puro
- el logging tendrá dos archivos:
  - `batch-summary.log`
  - `batch-detail.log`

Documentos disponibles:

- `docs/ARCHITECTURE.md`
- `docs/PROJECT_STRUCTURE.md`
- `docs/TECH_STACK.md`
- `docs/CONFIGURATION.md`
- `docs/LOGGING.md`
- `docs/BATCH_FLOW.md`
- `docs/MIGRATION_SCOPE.md`
- `docs/FILE_LIFECYCLE_AND_NOTIFICATIONS.md`
- `docs/LEGACY_PROCESSING_RULES.md`
- `docs/VALIDATION_AND_NORMALIZATION_RULES.md`
