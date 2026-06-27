/*
Función: dbo.ufnNormalizeCatalogCategoryName
Objetivo:
  - Construir una clave estable de comparación para nombres de categoría.
  - Centralizar la lógica de normalización para que el SP de consulta exponga
    tanto el nombre original como el valor normalizado que usa el batch.

Reglas de normalización:
  - NULL se convierte en cadena vacía.
  - Recorta espacios al inicio y al final.
  - Convierte tabulaciones, CR y LF en espacios.
  - Lleva el texto a mayúsculas.
  - Reemplaza vocales acentuadas y Ñ por equivalentes ASCII.
  - Colapsa espacios internos repetidos en uno solo.
*/
CREATE OR ALTER FUNCTION [dbo].[ufnNormalizeCatalogCategoryName]
(
    @CategoryName NVARCHAR(1024)
)
RETURNS NVARCHAR(1024)
AS
BEGIN
    DECLARE @Work NVARCHAR(1024);

    SET @Work = UPPER(LTRIM(RTRIM(ISNULL(@CategoryName, N''))));
    SET @Work = REPLACE(@Work, CHAR(9), N' ');
    SET @Work = REPLACE(@Work, CHAR(10), N' ');
    SET @Work = REPLACE(@Work, CHAR(13), N' ');
    SET @Work = TRANSLATE(@Work, N'ÁÀÄÂÃÉÈËÊÍÌÏÎÓÒÖÔÕÚÙÜÛÑ', N'AAAAAEEEEIIIIOOOOOUUUUN');

    WHILE CHARINDEX(N'  ', @Work) > 0
        SET @Work = REPLACE(@Work, N'  ', N' ');

    RETURN LTRIM(RTRIM(@Work));
END;
