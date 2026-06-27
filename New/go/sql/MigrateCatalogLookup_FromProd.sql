/*
Script: MigrateCatalogLookup_FromProd.sql
Objetivo:
  - Copiar desde producción hacia testing las tablas que usa
    `dbo.CatalogCategoryBranchLookup_Get`.
  - Borrar por completo el contenido actual de TEST y dejarlo idéntico a PROD.
  - Conservar exactamente los IDs/identities de producción.
  - Reemplazar los objetos SQL viejos por la función y el SP nuevos.

MUY IMPORTANTE:
  - Este script está pensado para ejecutarse parado en la base
    `StockCentral_Testing`.
  - NO modifica producción.
  - Solo lee desde producción a través de un linked server.

Origen:
  - Linked server: cambiar `@SourceLinkedServer` por el nombre real del linked server ya creado.
  - Base origen: `StockCentral`

Destino:
  - Base destino esperada: `StockCentral_Testing`
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SourceLinkedServer SYSNAME = N'REEMPLAZAR_POR_NOMBRE_DEL_LINKED_SERVER';
DECLARE @SourceDatabase SYSNAME = N'StockCentral';
DECLARE @SourceSchema SYSNAME = N'dbo';
DECLARE @TargetDatabase SYSNAME = N'StockCentral_Testing';
DECLARE @TargetSchema SYSNAME = N'dbo';

IF DB_NAME() <> @TargetDatabase
BEGIN
    THROW 50020, 'Este script debe ejecutarse en la base StockCentral_Testing. Abortado para evitar tocar otra base.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.servers
    WHERE name = @SourceLinkedServer
)
BEGIN
    THROW 50021, 'No existe el linked server indicado en @SourceLinkedServer. Revisalo antes de ejecutar.', 1;
END;

DECLARE @Tables TABLE
(
    TableName SYSNAME NOT NULL,
    DeleteOrder INT NOT NULL,
    InsertOrder INT NOT NULL
);

/*
Orden de borrado:
  1. Catalogo_Menu
  2. Menu_CategoriasRubro
  3. Rubro
  4. CategoriasRubro

Orden de inserción:
  1. CategoriasRubro
  2. Rubro
  3. Menu_CategoriasRubro
  4. Catalogo_Menu
*/
INSERT INTO @Tables (TableName, DeleteOrder, InsertOrder)
VALUES
    (N'Catalogo_Menu', 1, 4),
    (N'Menu_CategoriasRubro', 2, 3),
    (N'Rubro', 3, 2),
    (N'CategoriasRubro', 4, 1);

DECLARE
    @TableName SYSNAME,
    @QualifiedTarget NVARCHAR(300),
    @QualifiedSource NVARCHAR(500),
    @ColumnList NVARCHAR(MAX),
    @SelectList NVARCHAR(MAX),
    @IdentityColumn SYSNAME,
    @ReseedValue BIGINT,
    @SQL NVARCHAR(MAX);

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    -- Paso 1. Vaciar completamente las tablas de TEST.
    -- Esto toca solo la base destino donde corre el script.
    -------------------------------------------------------------------------
    DECLARE delete_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName
        FROM @Tables
        ORDER BY DeleteOrder;

    OPEN delete_cursor;
    FETCH NEXT FROM delete_cursor INTO @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @QualifiedTarget = QUOTENAME(@TargetSchema) + N'.' + QUOTENAME(@TableName);
        SET @SQL = N'DELETE FROM ' + @QualifiedTarget + N';';
        EXEC sys.sp_executesql @SQL;

        FETCH NEXT FROM delete_cursor INTO @TableName;
    END;

    CLOSE delete_cursor;
    DEALLOCATE delete_cursor;

    -------------------------------------------------------------------------
    -- Paso 2. Reinsertar exactamente lo que existe en PROD.
    -- Se usan los mismos valores de identity/PK que hay en origen.
    -------------------------------------------------------------------------
    DECLARE insert_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName
        FROM @Tables
        ORDER BY InsertOrder;

    OPEN insert_cursor;
    FETCH NEXT FROM insert_cursor INTO @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @QualifiedTarget = QUOTENAME(@TargetSchema) + N'.' + QUOTENAME(@TableName);
        SET @QualifiedSource = QUOTENAME(@SourceLinkedServer) + N'.' + QUOTENAME(@SourceDatabase) + N'.' + QUOTENAME(@SourceSchema) + N'.' + QUOTENAME(@TableName);

        SELECT
            @IdentityColumn = c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(@QualifiedTarget)
          AND c.is_identity = 1;

        SELECT
            @ColumnList = STRING_AGG(QUOTENAME(c.name), N', '),
            @SelectList = STRING_AGG(N'S.' + QUOTENAME(c.name), N', ')
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(@QualifiedTarget)
          AND c.is_computed = 0;

        SET @SQL = N'';

        IF @IdentityColumn IS NOT NULL
        BEGIN
            SET @SQL += N'SET IDENTITY_INSERT ' + @QualifiedTarget + N' ON;' + CHAR(10);
        END;

        SET @SQL += N'
INSERT INTO ' + @QualifiedTarget + N' (' + @ColumnList + N')
SELECT ' + @SelectList + N'
FROM ' + @QualifiedSource + N' AS S;';

        IF @IdentityColumn IS NOT NULL
        BEGIN
            SET @SQL += CHAR(10) + N'SET IDENTITY_INSERT ' + @QualifiedTarget + N' OFF;';
        END;

        EXEC sys.sp_executesql @SQL;

        /*
        Ajustamos el seed al valor máximo actual para que futuros INSERT locales
        continúen después del último ID copiado desde producción.
        */
        IF @IdentityColumn IS NOT NULL
        BEGIN
            SET @SQL = N'SELECT @MaxValue = MAX(' + QUOTENAME(@IdentityColumn) + N') FROM ' + @QualifiedTarget + N';';
            SET @ReseedValue = NULL;
            EXEC sys.sp_executesql @SQL, N'@MaxValue BIGINT OUTPUT', @MaxValue = @ReseedValue OUTPUT;

            IF @ReseedValue IS NOT NULL
            BEGIN
                SET @SQL = N'DBCC CHECKIDENT (''' + @TargetSchema + N'.' + @TableName + N''', RESEED, ' + CAST(@ReseedValue AS NVARCHAR(30)) + N') WITH NO_INFOMSGS;';
                EXEC sys.sp_executesql @SQL;
            END;
        END;

        FETCH NEXT FROM insert_cursor INTO @TableName;
    END;

    CLOSE insert_cursor;
    DEALLOCATE insert_cursor;

    -------------------------------------------------------------------------
    -- Paso 3. Borrar objetos SQL viejos del enfoque anterior, si existen.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.spGetSubCategoryByIntegratorCategoryName', N'P') IS NOT NULL
        DROP PROCEDURE dbo.spGetSubCategoryByIntegratorCategoryName;

    IF OBJECT_ID(N'dbo.ProviderCategoryNameToRubroId_Get', N'P') IS NOT NULL
        DROP PROCEDURE dbo.ProviderCategoryNameToRubroId_Get;

    IF OBJECT_ID(N'dbo.ufnNormalizeProviderCategoryName', N'FN') IS NOT NULL
        DROP FUNCTION dbo.ufnNormalizeProviderCategoryName;

    -------------------------------------------------------------------------
    -- Paso 4. Crear/actualizar la función vigente de normalización.
    -------------------------------------------------------------------------
    EXEC(N'
CREATE OR ALTER FUNCTION [dbo].[ufnNormalizeCatalogCategoryName]
(
    @CategoryName NVARCHAR(1024)
)
RETURNS NVARCHAR(1024)
AS
BEGIN
    DECLARE @Work NVARCHAR(1024);

    SET @Work = UPPER(LTRIM(RTRIM(ISNULL(@CategoryName, N''''))));
    SET @Work = REPLACE(@Work, CHAR(9), N'' '');
    SET @Work = REPLACE(@Work, CHAR(10), N'' '');
    SET @Work = REPLACE(@Work, CHAR(13), N'' '');
    SET @Work = TRANSLATE(@Work, N''ÁÀÄÂÃÉÈËÊÍÌÏÎÓÒÖÔÕÚÙÜÛÑ'', N''AAAAAEEEEIIIIOOOOOUUUUN'');

    WHILE CHARINDEX(N''  '', @Work) > 0
        SET @Work = REPLACE(@Work, N''  '', N'' '');

    RETURN LTRIM(RTRIM(@Work));
END;');

    -------------------------------------------------------------------------
    -- Paso 5. Crear/actualizar el SP vigente de lookup de categorías.
    -------------------------------------------------------------------------
    EXEC(N'
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
      AND LTRIM(RTRIM(ISNULL(r.descripcionrubroii, N''''))) <> N''''
    -- Ordenamos por nombre para que el resultset sea estable y legible.
    ORDER BY Name;
END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'delete_cursor') >= -1
    BEGIN
        CLOSE delete_cursor;
        DEALLOCATE delete_cursor;
    END;

    IF CURSOR_STATUS('local', 'insert_cursor') >= -1
    BEGIN
        CLOSE insert_cursor;
        DEALLOCATE insert_cursor;
    END;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
