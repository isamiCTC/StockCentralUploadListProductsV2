USE StockCentral_Testing
GO

ALTER PROCEDURE dbo.ProviderCategoryNameToRubroId_Get
(
    @ProviderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UPPER(LTRIM(RTRIM(ProviderCategoryName))) AS ProviderCategoryName,
        rubroid
    FROM ProviderCategoryNameToRubroId
    WHERE ProviderId IN (@ProviderId, 0)
      AND ProviderId = CASE
                           WHEN EXISTS (
                               SELECT 1
                               FROM ProviderCategoryNameToRubroId
                               WHERE ProviderId = @ProviderId
                           )
                           THEN @ProviderId
                           ELSE 0
                       END
    ORDER BY ProviderCategoryName;
END
