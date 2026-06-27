/*
Stored Procedure: dbo.CatalogCategoryBranchLookup_Get
Objetivo:
  - Devolver la lista de ramas de categoría válidas para un catálogo.
  - Exponer exactamente el dataset que necesita el batch Go para resolver
    `SUB CATEGORIA` desde Excel sin llamar a la API de subcategorías ni usar
    el mapping viejo `ProviderCategoryNameToRubroId`.

Contrato de salida:
  - Code:           Identificador de rubro/categoría usado en los payloads.
  - Name:           Nombre legible de la categoría esperado por negocio.
  - NormalizedName: Clave de comparación generada con dbo.ufnNormalizeCatalogCategoryName.

Notas:
  - Solo devuelve relaciones menú/catálogo habilitadas.
  - DISTINCT evita duplicados generados por relaciones repetidas del catálogo.
*/
CREATE OR ALTER PROCEDURE [dbo].[CatalogCategoryBranchLookup_Get]
    @CatalogoId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Armamos el dataset final directamente desde las tablas de catálogo.
    -- `Code` sale de `idrubro`, que es la PK del rubro.
    SELECT DISTINCT
        CAST(r.idrubro AS NVARCHAR(50)) AS Code,
        LTRIM(RTRIM(r.descripcionrubroii)) AS Name,
        dbo.ufnNormalizeCatalogCategoryName(r.descripcionrubroii) AS NormalizedName
    FROM dbo.Rubro AS r
    -- Cada rubro pertenece a una categoría padre.
    INNER JOIN dbo.CategoriasRubro AS cr
        ON r.category = cr.CategoriaRubroID
    -- El menú de categorías debe estar habilitado.
    INNER JOIN dbo.Menu_CategoriasRubro AS mcr
        ON cr.menuID = mcr.id
       AND mcr.enabled = 1
    -- Filtramos por catálogo habilitado y por el menú de productos.
    -- `cm.idmenu = 60` corresponde al menú de Productos.
    INNER JOIN dbo.Catalogo_Menu AS cm
        ON mcr.id = cm.idmenu
       AND cm.idcatalogo = @CatalogoId
       AND cm.enabled = 1
       AND cm.idmenu = 60
    -- Excluimos rubros borrados lógicamente.
    -- No devolvemos nombres vacíos porque no sirven para resolver subcategorías.
    WHERE r.deleted = 0
      AND LTRIM(RTRIM(ISNULL(r.descripcionrubroii, N''))) <> N''
    -- Ordenamos por nombre para que el resultset sea estable y legible.
    ORDER BY Name;
END;
