# SQL

Esta seccion documenta scripts SQL nuevos al 100% requeridos por la version V2.

## Stored Procedures

- `sql/ProviderCategoryNameToRubroId_Get.sql`
  - SP: `dbo.ProviderCategoryNameToRubroId_Get`
  - Base objetivo: `StockCentral_Testing`
  - Objetivo: resolver `ProviderCategoryName -> rubroid` priorizando `ProviderId` especifico y usando fallback a `ProviderId = 0`.
