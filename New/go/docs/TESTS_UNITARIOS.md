# Tests Unitarios del Proyecto Nuevo

## Objetivo de este documento

Este documento enumera los tests unitarios que existen hoy en el proyecto nuevo, explica qué valida cada uno y deja claro qué parte del comportamiento está protegida por tests automáticos.

La idea no es solo listar nombres. También busca responder:

- qué escenario prueba cada test;
- qué regla de negocio o de infraestructura protege;
- y qué riesgo ayuda a evitar si alguien cambia el código más adelante.

---

## Alcance

Este inventario cubre los tests unitarios actuales dentro de `New/go`.

No cubre:

- tests de integración contra SQL Server real;
- tests end-to-end con filesystem completo;
- pruebas manuales u operativas;
- ni validaciones del legacy en C#.

---

## Cómo correrlos

Desde la carpeta `New/go`:

```bash
go test ./...
```

Si querés correr solo un paquete:

```bash
go test ./internal/workbook/...
go test ./internal/products/...
```

---

## Resumen general

Hoy existen **35 tests unitarios**.

Están concentrados en estas áreas:

- `internal/batch`
- `internal/config`
- `internal/intake`
- `internal/notifications`
- `internal/products`
- `internal/results`
- `internal/workbook`

En términos prácticos, hoy se cubren especialmente:

- reglas de parsing y validación de Excel;
- semántica legacy de producto e imágenes;
- movimientos de archivos;
- escritura de Excels de salida;
- carga de configuración y secretos;
- resolución de destinatarios y notificación;
- algunos caminos críticos de timeout por fila.

---

## Enfoque de los tests

La mayoría usa técnicas típicas de tests unitarios en Go:

- `httptest` para simular APIs HTTP sin salir a red real;
- `t.TempDir()` para crear carpetas y archivos temporales;
- stubs simples para reemplazar componentes externos;
- y checks directos sobre structs, archivos generados o errores devueltos.

Eso significa que estos tests son rápidos, aislados y no dependen de infraestructura real.

---

## Inventario por paquete

## `internal/batch`

Archivo: `internal/batch/file_processor_test.go`

### `TestNewFileProcessorAppliesDefaults`

Qué prueba:

- que `NewFileProcessor` complete valores por defecto cuando recibe `0` o valores vacíos.

Qué valida:

- `rowWorkers` mínimo en `1`;
- `rowTimeout` por defecto en `120s`.

Por qué importa:

- evita que una mala configuración deje el procesador en un estado inválido.

### `TestSummarizeRowResultsIgnoresSkippedAndCountsFinalStates`

Qué prueba:

- que el resumen final de filas ignore las `SKIPPED` y cuente bien `OK`, `PARTIAL_OK` y `ERROR`.

Qué valida:

- cantidad de filas procesadas;
- cantidad de éxitos;
- cantidad de parciales;
- cantidad de errores.

Por qué importa:

- protege la consolidación del resultado final del archivo.

### `TestResolveFileStatusReturnsProcessedWithErrorsWhenAnyRowFailedOrPartial`

Qué prueba:

- la regla que decide el estado final del archivo.

Qué valida:

- si no hay errores ni parciales, el archivo queda `PROCESSED`;
- si hay al menos un error o parcial, queda `PROCESSED_WITH_ERRORS`.

Por qué importa:

- asegura consistencia entre resultados por fila y estado global del archivo.

### `TestProcessMappedRowsMarksStockRowAsErrorWhenRowTimeoutExpires`

Qué prueba:

- un timeout real de fila durante un flujo de stock update.

Cómo lo hace:

- monta un servidor HTTP fake que responde lento;
- usa un `rowTimeout` muy chico;
- procesa una fila de stock.

Qué valida:

- la fila termina en `ERROR`;
- el mensaje final informa timeout.

Por qué importa:

- protege uno de los caminos operativos más delicados: que una fila lenta no quede “colgada”.

### `TestProcessFullImportRowMarksPartialWhenTimeoutExpiresDuringImages`

Qué prueba:

- el caso donde el producto ya quedó impactado, pero la fila vence durante la sincronización de imágenes.

Cómo lo hace:

- simula una API de producto rápida;
- simula un servidor de imágenes muy lento;
- procesa una fila full import con imágenes.

Qué valida:

- la fila termina en `PARTIAL_OK`;
- `ProductResult` queda `ACTUALIZADO`;
- `ImagesResult` queda `PARCIAL`;
- el mensaje refleja que el timeout ocurrió durante imágenes.

Por qué importa:

- protege la semántica parcial correcta del proceso.

---

## `internal/config`

Archivo: `internal/config/loader_test.go`

### `TestLoadReadsSecretsFromEnvFile`

Qué prueba:

- que `Load` lea correctamente secretos desde `.env`.

Qué valida:

- `DB_CONNECTION_STRING`;
- `PRODUCTS_API_TOKEN`;
- `SENDGRID_API_KEY`.

Por qué importa:

- asegura que la configuración final se arme correctamente con TOML + `.env`.

### `TestLoadFailsWhenProductsTokenIsMissing`

Qué prueba:

- que falle la carga si falta `PRODUCTS_API_TOKEN`.

Qué valida:

- que devuelva error;
- que el mensaje mencione el faltante correcto.

Por qué importa:

- evita arrancar el batch sin token de API de productos.

### `TestLoadFailsWhenNotificationsAreEnabledButSendGridKeyIsMissing`

Qué prueba:

- que falle la carga cuando notificaciones están activadas pero falta `SENDGRID_API_KEY`.

Qué valida:

- que la validación sea condicional a `notifications.enabled = true`.

Por qué importa:

- protege un caso de configuración inconsistente que después rompería el envío de mails.

---

## `internal/intake`

Archivo: `internal/intake/scanner_test.go`

### `TestDiscoverProviderFilesFiltersByProviderAndExtensionAndKeepsRelativePath`

Qué prueba:

- el descubrimiento real de archivos candidatos en `input_root`.

Qué valida:

- solo entra el provider habilitado;
- solo entran `.xlsx`;
- se permiten subdirectorios;
- se conserva `RelativePath`;
- se copia correctamente `ProviderID`, `ProviderName` y `ProviderEmail`.

Por qué importa:

- protege una de las reglas centrales del sistema: qué archivos se consideran válidos para procesar.

### `TestDiscoverProviderFilesStopsWhenContextIsCanceled`

Qué prueba:

- que el scanner respete cancelación del contexto.

Qué valida:

- si el contexto ya está cancelado, devuelve `context.Canceled`.

Por qué importa:

- garantiza comportamiento correcto ante corte externo de la corrida.

Archivo: `internal/intake/mover_test.go`

### `TestBuildPathsPreservesProviderAndRelativeStructure`

Qué prueba:

- cómo se derivan las rutas de `processing`, `processed`, `result` y `structure-errors`.

Qué valida:

- que se conserve el provider;
- que se conserve la subruta relativa;
- que los sufijos de salida queden bien armados.

Por qué importa:

- protege la consistencia del ciclo de vida del archivo.

### `TestMoveToProcessingAndProcessedMovesFileAndUpdatesInputPath`

Qué prueba:

- los movimientos reales de archivo entre estados.

Qué valida:

- el archivo se mueve a `processing`;
- desaparece del origen;
- luego se mueve a `processed`;
- `InputPath` se actualiza en cada paso.

Por qué importa:

- evita desalineación entre el archivo físico y el estado lógico del `FileJob`.

---

## `internal/notifications`

Archivo: `internal/notifications/recipients_test.go`

### `TestResolveRecipientsDeduplicatesAndTrims`

Qué prueba:

- la resolución final de destinatarios.

Qué valida:

- trim de espacios;
- deduplicación case-insensitive;
- combinación entre `always_recipients` y mail del provider.

Por qué importa:

- evita enviar mails duplicados o con direcciones sucias.

Archivo: `internal/notifications/service_test.go`

### `TestNotifyFileProcessedSkipsWhenNotificationsDisabled`

Qué prueba:

- que el servicio no intente enviar nada si las notificaciones están apagadas.

Qué valida:

- cero llamadas al sender.

Por qué importa:

- evita efectos secundarios cuando la funcionalidad está deshabilitada por config.

### `TestNotifyFileProcessedSendsExpectedAttachmentForProcessedWithErrors`

Qué prueba:

- el caso normal de archivo procesado con errores.

Qué valida:

- `from_email`;
- destinatarios finales;
- subject;
- attachment de `ResultsFilePath`.

Por qué importa:

- protege el contrato externo más visible del cierre del archivo.

### `TestNotifyFileProcessedUsesStructureAttachmentForStructureError`

Qué prueba:

- el caso de rechazo estructural.

Qué valida:

- que use `StructureErrorsPath` como adjunto.

Por qué importa:

- evita mandar el archivo equivocado cuando el Excel se rechaza por estructura.

### `TestNotifyFileProcessedReturnsWrappedSenderError`

Qué prueba:

- el comportamiento cuando el sender falla.

Qué valida:

- que el error no se silencie.

Por qué importa:

- permite que capas superiores logueen o reaccionen ante el fallo del correo.

---

## `internal/products`

Archivo: `internal/products/products_test.go`

### `TestUpsertProductLegacyCreatesWhenProductDoesNotExist`

Qué prueba:

- la semántica legacy de upsert de producto.

Cómo lo hace:

- finge un `PUT` que responde `Producto inexistente`;
- luego espera un `POST`.

Qué valida:

- que el resultado final sea `CREATE`;
- que se haga primero `PUT` y después `POST`.

Por qué importa:

- protege una de las reglas más importantes heredadas del legacy.

### `TestUpsertProductLegacyFailsWhenCreateReturnsNon2xx`

Qué prueba:

- el caso donde el fallback a create también falla.

Qué valida:

- que `UpsertProductLegacy` devuelva error si el `POST` no es exitoso.

Por qué importa:

- evita falsos positivos de “producto creado” cuando la API realmente falló.

Archivo: `internal/products/images_test.go`

### `TestSyncImageLegacySkipsWhenBase64IsEqual`

Qué prueba:

- que no se vuelva a subir una imagen si la existente ya es igual.

Qué valida:

- `Action = SKIP_SAME_IMAGE`;
- no se usa `PUT` ni `POST`.

Por qué importa:

- protege eficiencia y fidelidad con la lógica legacy.

### `TestSyncImageLegacyCreatesWhenPutSaysImageNotFound`

Qué prueba:

- la semántica legacy de imágenes cuando el índice todavía no existe.

Qué valida:

- primero intenta `PUT`;
- si la API responde `34|Imagen inexistente`, hace `POST`;
- el resultado final es `CREATE`.

Por qué importa:

- protege exactamente el comportamiento que se quería mantener respecto del legacy.

### `TestSyncImageLegacyFailsWhenUpdateReturnsNon2xx`

Qué prueba:

- que falle la sincronización si el `PUT` da un error no recuperable.

Qué valida:

- que no se silencien errores de update.

Por qué importa:

- evita marcar imágenes como sincronizadas cuando la API respondió mal.

---

## `internal/results`

Archivo: `internal/results/writer_test.go`

### `TestWriteRowResultsOmitsSkippedRowsAndWritesExpectedSheet`

Qué prueba:

- la escritura del Excel `Resultados`.

Qué valida:

- la hoja se llama `Resultados`;
- las filas `SKIPPED` no se escriben;
- los datos útiles quedan en celdas esperadas.

Por qué importa:

- protege el formato del archivo que recibe negocio.

### `TestWriteStructureErrorsWritesExpectedRows`

Qué prueba:

- la escritura del Excel `ErroresEstructura`.

Qué valida:

- la hoja se llama `ErroresEstructura`;
- campo, mensaje y detalle quedan en columnas correctas.

Por qué importa:

- asegura que el rechazo estructural sea legible para usuario final.

---

## `internal/workbook`

Archivo: `internal/workbook/normalize_test.go`

### `TestNormalizeHeader`

Qué prueba:

- la normalización laxa de headers.

Qué valida:

- trim;
- colapso de espacios;
- remoción de diferencias como tildes y formato superficial.

Por qué importa:

- permite tolerar pequeñas variaciones humanas en encabezados.

### `TestNormalizeCell`

Qué prueba:

- la normalización básica de una celda.

Qué valida:

- recorte de espacios exteriores.

Por qué importa:

- evita errores tontos por valores con padding.

Archivo: `internal/workbook/validator_test.go`

### `TestValidateStructureAcceptsLaxHeaders`

Qué prueba:

- que la validación estructural acepte headers válidos aunque vengan con diferencias cosméticas.

Qué valida:

- matching por header normalizado;
- indexación correcta de columnas.

Por qué importa:

- protege la estrategia de validación flexible acordada para el batch.

### `TestValidateStructureReportsMissingColumns`

Qué prueba:

- la detección de columnas faltantes.

Qué valida:

- que devuelva el error sobre el campo correcto.

Por qué importa:

- evita que un Excel incompleto avance a etapas más costosas.

### `TestValidateStructureReportsDuplicates`

Qué prueba:

- la detección de columnas duplicadas.

Qué valida:

- que el error se marque con el mensaje esperado.

Por qué importa:

- protege contra archivos ambiguos o mal exportados.

Archivo: `internal/workbook/numbers_test.go`

### `TestParseFlexibleFloat`

Qué prueba:

- varias variantes de números decimales frecuentes en proveedores.

Qué valida:

- punto decimal;
- coma decimal;
- miles con punto;
- miles con coma;
- espacios.

Por qué importa:

- protege el parsing flexible del importador.

### `TestParseFlexibleFloatInvalid`

Qué prueba:

- que una cadena no numérica falle.

Qué valida:

- que no se acepte basura como número.

Por qué importa:

- evita que datos inválidos pasen como si fueran correctos.

### `TestParseFlexibleInt`

Qué prueba:

- parsing válido de enteros.

Qué valida:

- conversión correcta a `int`.

Por qué importa:

- protege campos como stock y otros enteros obligatorios.

### `TestParseFlexibleIntRejectsDecimalValue`

Qué prueba:

- que un valor decimal no se acepte como entero.

Qué valida:

- rechazo explícito de `12,5` como `int`.

Por qué importa:

- evita truncamientos silenciosos.

Archivo: `internal/workbook/mapper_test.go`

### `TestMapRowsFullImportHappyPath`

Qué prueba:

- el mapeo completo exitoso de una fila de importación de 19 columnas.

Qué valida:

- se crea el DTO `FullImport`;
- no hay errores de fila;
- `WeightKilograms` se convierte desde gramos a kilogramos;
- `OFERTA` pisa `Price`;
- `IVA` se normaliza a porcentaje;
- `SyncImages` queda en `true`;
- las URLs de imagen se separan bien y quedan dos.

Por qué importa:

- protege el camino funcional principal del import full.

### `TestMapRowsFullImportInvalidImageURLProducesError`

Qué prueba:

- una fila full import con URL de imagen inválida.

Qué valida:

- la fila queda con errores;
- el mapper no deja pasar imágenes mal formadas.

Por qué importa:

- evita llegar a la API de imágenes con input roto desde el Excel.

---

## Lectura rápida: qué partes están mejor cubiertas

Hoy la cobertura conceptual más fuerte está en:

- semántica legacy de producto;
- semántica legacy de imágenes;
- parsing y validación del workbook;
- generación de archivos de salida;
- reglas de notificación;
- timeouts críticos de fila.

---

## Qué no está cubierto por unit tests hoy

Hoy no se ve cobertura unitaria específica, por ejemplo, para:

- CLI y subcomandos;
- `self-check`;
- lectura real de Excel desde archivos de entrada complejos;
- integración real con SQL Server;
- integración real con SendGrid;
- logging como salida observable;
- `catalog/resolver` con casos propios dedicados;
- `products/downloader` con batería específica de formatos de imagen.

Esto no significa que esté mal. Solo marca qué partes hoy dependen más de revisión manual, lectura de código o futuras pruebas de integración.

---

## Conclusión

La suite actual de tests unitarios no es enorme, pero sí está bastante bien orientada: cubre reglas delicadas, caminos legacy importantes y varios puntos donde un cambio pequeño podría romper comportamiento esperado.

En especial, protege bien:

- cómo entra y se valida el Excel;
- cómo se decide create/update en producto e imágenes;
- cómo se arma el resultado que ve negocio;
- y cómo reacciona el sistema ante fallos operativos básicos.

Como inventario operativo, hoy este documento sirve para saber qué ya está blindado por tests y qué sigue más apoyado en validación manual o pruebas de integración futuras.
