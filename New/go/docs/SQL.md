# SQL

Esta seccion documenta los scripts SQL vigentes requeridos por la version V2.

Hoy el repositorio conserva solamente los objetos SQL activos del flujo actual.
No quedan en esta carpeta scripts legacy de mapping viejo ni scripts auxiliares
de migracion de datos entre ambientes.

## Funcion

- `sql/ufnNormalizeCatalogCategoryName.sql`
  - Funcion: `dbo.ufnNormalizeCatalogCategoryName`
  - Objetivo: construir una clave estable de comparacion para nombres de categoria.
  - Uso actual: normalizar el lado SQL del dataset de categorias validas del catalogo.

## Stored Procedures

- `sql/CatalogCategoryBranchLookup_Get.sql`
  - SP: `dbo.CatalogCategoryBranchLookup_Get`
  - Parametro principal: `@CatalogoId`
  - Objetivo: devolver las ramas validas de categoria para un catalogo.
  - Contrato de salida:
    - `Code`
    - `Name`
    - `NormalizedName`
  - Uso actual: precargar en memoria la cache de resolucion de `SUB CATEGORIA`.

## Diferencia con el enfoque anterior

- La V2 ya no depende del mapping `ProviderCategoryName -> RubroId`.
- La V2 ya no consulta la API de subcategorias para completar la resolucion.
- La resolucion vigente parte de un dataset SQL del catalogo configurado y cae a fallback solo si no encuentra match.
