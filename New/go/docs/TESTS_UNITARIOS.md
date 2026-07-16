# Tests Unitarios del Proyecto Nuevo

## Objetivo de este documento

Este documento inventaria los tests unitarios actuales de `New/go`, explica qué protege cada grupo y deja claro qué partes del comportamiento real de V2 hoy están cubiertas por automatización.

La referencia para validar si algo sigue vigente es el código actual. Si la implementación cambia, este archivo debe acompañar ese cambio.

---

## Alcance

Este inventario cubre los tests unitarios hoy presentes dentro de `New/go`.

No cubre:

- tests de integración contra SQL Server real;
- tests end-to-end contra filesystem real completo;
- corridas reales contra Products API;
- envío real de mails;
- ni validaciones del legacy en C#.

---

## Cómo correrlos

La forma oficial de correr la suite desde `New/go` es:

```powershell
./scripts/test.ps1
```

Ese script:

- ejecuta `go test -count=1 ./...`;
- muestra la salida real mientras corre;
- y cierra con un resumen final más legible.

También se puede usar `go test` directo para ciclos puntuales. Por ejemplo:

```powershell
go test ./...
go test ./internal/workbook/...
go test ./internal/integrations/productsapi/...
go test ./internal/integrations/sqlserver/...
```

---

## Resumen general

Hoy existen **60 tests unitarios**.

Están repartidos en estas áreas:

- `internal/batch`
- `internal/catalog`
- `internal/config`
- `internal/images`
- `internal/intake`
- `internal/logging`
- `internal/notifications`
- `internal/integrations/productsapi`
- `internal/integrations/sqlserver`
- `internal/reporting`
- `internal/results`
- `internal/workbook`

En términos prácticos, hoy la suite cubre especialmente:

- reglas de parsing y validación de Excel;
- semántica legacy de producto, stock e imágenes;
- resolución de categorías contra cache SQL + fallback configurado;
- movimientos de archivos;
- escritura de Excels de salida;
- carga de configuración y secretos;
- presentación final de resultados y humanización de errores;
- resolución de destinatarios y notificación;
- caminos sensibles de timeout por fila.

---

## Enfoque de los tests

La suite usa principalmente:

- `httptest` para simular APIs HTTP sin salir a red real;
- `t.TempDir()` para crear carpetas y archivos temporales;
- stubs simples para reemplazar componentes externos;
- checks directos sobre structs, archivos generados y errores devueltos.

Eso hace que los tests sean rápidos, aislados y sin dependencia de infraestructura real.

---

## Inventario por paquete

## `internal/batch`

Archivo: `internal/batch/file_processor_test.go`

### `TestNewFileProcessorAppliesDefaults`

Valida que `NewFileProcessor` complete defaults seguros cuando recibe valores vacíos o inválidos.

### `TestSummarizeRowResultsIgnoresSkippedAndCountsFinalStates`

Valida que el resumen final ignore filas `SKIPPED` y consolide correctamente `OK`, `PARTIAL_OK` y `ERROR`.

### `TestResolveFileStatusReturnsProcessedWithErrorsWhenAnyRowFailedOrPartial`

Protege la regla que decide si el archivo termina en `PROCESSED` o `PROCESSED_WITH_ERRORS`.

### `TestProcessMappedRowsMarksStockRowAsErrorWhenRowTimeoutExpires`

Cubre el timeout por fila en un flujo de stock update y verifica que la fila termine en `ERROR`.

### `TestProcessStockUpdateRowHumanizesAPIErrorDetail`

Verifica que un error técnico de API en stock update se traduzca a detalle legible para el Excel final, sin dejar el mensaje crudo de infraestructura.

### `TestProcessFullImportRowMarksPartialWhenTimeoutExpiresDuringImages`

Cubre el caso donde el producto quedó impactado pero la fila vence durante la sincronización de imágenes, dejando la fila en `PARTIAL_OK`.

### `TestProcessFullImportRowMarksPartialWhenCategoryFallsBack`

Verifica que caer en la categoría general configurada no marque la fila como `OK` silenciosamente y que el resultado visible quede como observación parcial.

## `internal/catalog`

Archivo: `internal/catalog/resolver_test.go`

### `TestResolveBySubcategoryMatchesDatabaseMappingWithLooseNormalization`

Valida que la resolución por subcategoría funcione contra la cache precargada del catálogo usando normalización laxa.

### `TestResolveBySubcategoryFallsBackWhenDatabaseMappingDoesNotMatch`

Protege el fallback configurado cuando la subcategoría no matchea contra una rama válida del catálogo.

### `TestResolveBySubcategoryFallsBackWhenDatabaseMappingHasNoMatches`

Valida el caso extremo donde el dataset precargado no aporta ninguna coincidencia y el resolvedor igual responde con el fallback configurado.

## `internal/config`

Archivo: `internal/config/loader_test.go`

### `TestLoadReadsSecretsFromEnvFile`

Verifica que `Load` combine TOML y `.env` y levante correctamente los secretos requeridos.

### `TestLoadFailsWhenProductsTokenIsMissing`

Protege el fail-fast cuando falta `PRODUCTS_API_TOKEN`.

### `TestLoadFailsWhenNotificationsAreEnabledButSendGridKeyIsMissing`

Valida que `SENDGRID_API_KEY` sea obligatoria solo cuando las notificaciones están activadas.

## `internal/intake`

Archivo: `internal/intake/scanner_test.go`

### `TestDiscoverProviderFilesFiltersByProviderAndExtensionAndKeepsRelativePath`

Valida el descubrimiento de archivos `.xlsx` dentro de carpetas numéricas de providers válidos y la preservación de `RelativePath`.

### `TestDiscoverProviderFilesStopsWhenContextIsCanceled`

Protege que el scanner respete cancelación de contexto y corte la corrida de forma correcta.

Archivo: `internal/intake/mover_test.go`

### `TestBuildPathsPreservesProviderAndRelativeStructure`

Verifica el cálculo de rutas derivadas para `processing`, `processed`, `results` y `structure-errors`.

### `TestMoveToProcessingAndProcessedMovesFileAndUpdatesInputPath`

Cubre el movimiento real del archivo entre estados y la actualización de `InputPath`.

### `TestMoveToProcessingFallsBackToCopyAndRemoveOnCrossDeviceError`

Valida el fallback a copiar y borrar cuando un rename directo falla por cruce de volumen o device.

## `internal/notifications`

Archivo: `internal/notifications/recipients_test.go`

### `TestResolveRecipientsDeduplicatesAndTrims`

Protege trim, deduplicación case-insensitive y combinación entre `always_recipients` y mail del provider.

Archivo: `internal/notifications/service_test.go`

### `TestNotifyFileProcessedSkipsWhenNotificationsDisabled`

Verifica que el servicio no intente enviar nada si las notificaciones están desactivadas.

### `TestNotifyFileProcessedSendsExpectedAttachmentForProcessedWithErrors`

Valida el caso normal de archivo procesado con errores y el adjunto correcto del Excel de resultados.

### `TestNotifyFileProcessedUsesStructureAttachmentForStructureError`

Verifica que, ante error estructural, el adjunto sea `StructureErrorsPath`.

### `TestNotifyFileProcessedReturnsWrappedSenderError`

Protege que un fallo del sender no se silencie y conserve contexto útil.

## `internal/integrations/productsapi`

Archivo: `internal/integrations/productsapi/products_test.go`

### `TestUpsertProductLegacyCreatesWhenProductDoesNotExist`

Valida la semántica legacy de `PUT` seguido de `POST` cuando la API responde `Producto inexistente`.

### `TestUpsertProductLegacyFailsWhenCreateReturnsNon2xx`

Protege que un `POST` fallido no se informe como creación exitosa.

### `TestUpsertProductLegacyFailsWhenUpdateReturnsNon2xxAndIncludesBody`

Verifica que un fallo no recuperable del `PUT` devuelva error con contexto y body útil para diagnóstico.

### `TestUpsertProductLegacyRetriesUpdateWhenAPIReportsDeadlock`

Valida que un `PUT` que recibe el mensaje de interbloqueo se reintente y pueda recuperarse.

### `TestUpsertProductLegacyRetriesCreateWhenAPIReportsDeadlock`

Valida el mismo comportamiento para el `POST` posterior a `Producto inexistente`.

### `TestUpsertProductLegacyDoesNotRetryUnrelatedInternalServerError`

Confirma que un error `500` sin el mensaje específico no se reintente.

### `TestUpsertProductLegacyStopsAfterConfiguredDeadlockAttempts`

Confirma que los intentos terminen al alcanzar el máximo configurado.

Archivo: `internal/integrations/productsapi/images_test.go`

### `TestSyncImageLegacySkipsWhenBase64IsEqual`

Valida que no se resuba una imagen cuando el contenido existente ya es idéntico.

### `TestSyncImageLegacyCreatesWhenPutSaysImageNotFound`

Protege la semántica legacy de `PUT` seguido de `POST` cuando la imagen aún no existe en ese índice.

### `TestSyncImageLegacyFailsWhenUpdateReturnsNon2xx`

Verifica que un error no recuperable de update no se silencie.

### `TestSyncImageLegacyFailsWhenCreateReturnsNon2xxAndIncludesBody`

Valida que un fallo del `POST` de imagen preserve el contexto técnico importante para diagnóstico.

## `internal/results`

Archivo: `internal/results/writer_test.go`

### `TestWriteRowResultsOmitsSkippedRowsAndWritesExpectedSheet`

Protege la escritura del Excel `Resultados`, incluyendo la omisión de filas `SKIPPED`.

### `TestWriteStructureErrorsWritesExpectedRows`

Verifica la escritura del Excel `ErroresEstructura` con columnas y contenido esperados.

## `internal/reporting`

Archivo: `internal/reporting/row_outcome_builder_test.go`

### `TestBuildStockSuccessPresentation`

Valida el texto final visible para una fila de stock update exitosa.

### `TestBuildFullImportPresentationReturnsOKForUpdatedProductWithUnchangedImages`

Protege el caso donde el producto quedó bien y las imágenes ya estaban cargadas.

### `TestBuildFullImportPresentationReturnsPartialForFallbackCategory`

Verifica la presentación final cuando se usa la categoría fallback configurada.

### `TestBuildFullImportPresentationReturnsPartialForInterruptedImages`

Protege la presentación visible cuando el producto se impactó pero las imágenes quedaron parciales por interrupción o timeout.

Archivo: `internal/reporting/error_presentation_test.go`

### `TestBuildErrorPresentationHumanizesHTTPAPIError`

Valida la traducción de errores HTTP de API a mensajes legibles para negocio o cliente final.

### `TestBuildErrorPresentationHumanizesImageDownloadError`

Verifica la humanización de errores de descarga de imágenes sin exponer detalles técnicos innecesarios.

## `internal/images`

Archivo: `internal/images/downloader_test.go`

### `TestIsWebPImageDetectsContentType`

Valida la detección de WebP por `Content-Type`.

### `TestIsWebPImageDetectsRIFFHeader`

Valida la detección de WebP por firma binaria `RIFF ... WEBP`.

### `TestIsWebPImageIgnoresJPEGData`

Protege contra falsos positivos al distinguir imágenes JPEG de WebP.

## `internal/logging`

Archivo: `internal/logging/logger_test.go`

### `TestBufferFlushSeparatesBlockWithBlankLines`

Verifica el formato de los bloques buffered del log `detail`.

### `TestLoggerBlankWritesSingleEmptyLine`

Protege la escritura consistente de una línea vacía explícita usando el separador del sistema operativo.

## `internal/integrations/sqlserver`

Archivo: `internal/integrations/sqlserver/client_test.go`

### `TestQueryContextKeepsRowsUsableAfterReturn`

Valida que `QueryContext` no invalide el resultset apenas retorna y permita consumir filas correctamente.

## `internal/workbook`

Archivo: `internal/workbook/normalize_test.go`

### `TestNormalizeHeader`

Protege la normalización laxa de headers.

### `TestNormalizeCell`

Valida la limpieza conservadora de celdas vía trim externo.

Archivo: `internal/workbook/validator_test.go`

### `TestValidateStructureAcceptsLaxHeaders`

Verifica que la validación estructural acepte diferencias cosméticas de headers válidos.

### `TestValidateStructureReportsMissingColumns`

Protege la detección de columnas faltantes.

### `TestValidateStructureReportsDuplicates`

Protege la detección de columnas duplicadas.

Archivo: `internal/workbook/numbers_test.go`

### `TestParseFlexibleFloat`

Valida el parsing flexible de decimales con distintos separadores y formatos humanos frecuentes.

### `TestParseFlexibleFloatInvalid`

Verifica que una cadena no numérica falle como corresponde.

### `TestParseFlexibleInt`

Protege la conversión válida de enteros.

### `TestParseFlexibleIntRejectsDecimalValue`

Verifica que un decimal no se acepte silenciosamente como entero.

### `TestParseFlexibleIntWithCurrencySymbol`

Valida que el parser soporte enteros expresados con símbolo monetario cuando el input sigue siendo numéricamente válido.

Archivo: `internal/workbook/mapper_test.go`

### `TestMapRowsFullImportHappyPath`

Protege el camino principal del mapeo completo de 19 columnas, incluyendo peso, IVA, oferta e imágenes.

### `TestMapRowsFullImportAcceptsCurrencySymbolInPriceFields`

Valida que los campos monetarios acepten símbolos de moneda frecuentes sin romper el parsing.

### `TestMapRowsFullImportInvalidImageURLProducesError`

Verifica que una URL de imagen inválida corte la fila antes de llegar a sincronización.

### `TestMapRowsStockUpdateInvalidSKUProducesError`

Protege la validación de SKU en el formato corto de stock update.

### `TestMapRowsFullImportKeepsInvalidStartDateWithoutError`

Valida el comportamiento actual de V2: las fechas se conservan como texto crudo y no fallan por formato.

### `TestMapRowsFullImportKeepsInvertedDateRangeWithoutError`

Verifica que un rango invertido de fechas tampoco invalide la fila en el mapper actual.

---

## Lectura rápida: qué partes están mejor cubiertas

Hoy la cobertura conceptual más fuerte está en:

- semántica legacy de producto, stock e imágenes;
- resolución de categorías contra dataset SQL precargado y fallback;
- parsing, normalización y validación del workbook;
- mapeo de filas full import y stock update;
- movimientos de archivos;
- generación de archivos de salida;
- presentación final de resultados y errores;
- reglas de notificación;
- timeouts críticos por fila.

---

## Qué no está cubierto por unit tests hoy

Hoy no se ve cobertura unitaria específica, por ejemplo, para:

- CLI y subcomandos como comportamiento integral;
- `self-check`;
- lectura real de archivos Excel complejos desde disco;
- integración real con SQL Server;
- integración real con SendGrid;
- corrida batch completa de punta a punta;
- logging como artefacto operativo completo sobre archivos reales;
- descarga y conversión de imágenes con una batería más amplia de formatos y respuestas remotas.

Esto no implica que el proyecto esté mal cubierto. Solo marca qué partes hoy dependen más de lectura de código, validación manual o futuras pruebas de integración.

---

## Conclusión

La suite actual no es enorme, pero sí cubre varios puntos donde un cambio chico podría romper comportamiento esperado de V2.

En especial, hoy protege bien:

- cómo entra y se valida el Excel;
- cómo se decide update/create en producto e imágenes;
- cómo se resuelven categorías en el flujo actual;
- cómo se arma el resultado visible para negocio;
- y cómo reacciona el proceso ante errores operativos frecuentes.

Como inventario operativo, este documento sirve para entender qué comportamiento ya está blindado por tests unitarios y qué áreas todavía descansan más en validación manual o pruebas de integración.
