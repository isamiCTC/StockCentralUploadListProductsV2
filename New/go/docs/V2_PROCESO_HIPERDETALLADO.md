# StockCentralUploadListProductsV2: Informe Hiper Detallado

## Objetivo de este documento

Este documento describe, con el mayor detalle posible, cómo funciona hoy el proyecto nuevo `StockCentralUploadListProductsV2`.

La idea es dejar documentado el comportamiento real implementado en Go, no solamente la intención de diseño.

---

## Qué es `StockCentralUploadListProductsV2`

`StockCentralUploadListProductsV2` es una reescritura batch en Go del servicio legacy que procesaba Excels de productos.

La V2 cambia varias decisiones estructurales del sistema anterior:

1. deja de ser un Windows Service en loop infinito;
2. pasa a ser un batch de una sola corrida;
3. mueve la responsabilidad del scheduling hacia afuera;
4. procesa archivos de a uno;
5. procesa filas concurrentemente dentro de cada archivo;
6. genera un Excel de resultado por archivo;
7. envía un mail corto con el Excel final y además el archivo procesado;
8. centraliza mejor logging, paths, configuración y separación de responsabilidades.

En términos funcionales, el objetivo sigue siendo el mismo:

- detectar archivos Excel para providers válidos;
- leerlos;
- validarlos;
- transformar cada fila en una transacción lógica;
- impactar productos e imágenes en la API REST de productos;
- y dejar trazabilidad operativa y de negocio.

---

## Proyectos, paquetes y archivos clave

El flujo principal de V2 se reparte entre estos archivos:

### Entry point y orquestación

1. `cmd/StockCentralUploadListProductsV2/main.go`
   Punto de entrada mínimo. Solo delega la ejecución a la CLI.

2. `internal/cli/root.go`
   Define el comando raíz con Cobra, una librería de Go para CLIs con subcomandos, flags y help automático.

3. `internal/app/runbatch/runtime.go`
   Construye todas las dependencias concretas del modo batch y expone helpers de logging de inicio/cierre.

4. `internal/app/selfcheck/checks.go`
   Implementa el subcomando `self-check` y sus verificaciones técnicas de ambiente.

5. `internal/batch/processor.go`
   Orquestador principal del batch completo.

6. `internal/batch/file_processor.go`
   Procesador de un archivo individual.

### Configuración

6. `internal/config/loader.go`
   Carga `appsettings.toml` y `.env`, arma la configuración final y la valida.

7. `internal/config/settings.go`
   Modela las structs de configuración.

8. `config/appsettings.toml`
   Configuración no sensible.

### Providers y filesystem

9. `internal/providers/sqlserver.go`
   Conexión base a SQL Server.

10. `internal/providers/sqlserver_repository.go`
   Ejecución del SP de providers y mapping del resultset.

11. `internal/intake/scanner.go`
   Descubrimiento de archivos en input.

12. `internal/intake/mover.go`
    Movimiento de archivos entre `input`, `processing` y `processed`.

### Excel

13. `internal/workbook/reader.go`
    Apertura del `.xlsx`, lectura de la primera hoja y armado del `Workbook`.

14. `internal/workbook/validator.go`
    Validación estructural del archivo.

15. `internal/workbook/mapper.go`
    Conversión fila por fila a DTOs tipados.

16. `internal/workbook/numbers.go`
    Parsing numérico flexible.

17. `internal/workbook/normalize.go`
    Normalización laxa de headers y celdas.

### API de productos y categorías

18. `internal/products/client.go`
    Cliente base REST.

19. `internal/products/products.go`
    Operaciones de producto y upsert legacy.

20. `internal/products/images.go`
    Sincronización legacy de imágenes.

21. `internal/products/subcategories.go`
    Fallback REST de subcategorías.

22. `internal/catalog/resolver.go`
    Resolución de categoría a partir de subcategoría.

23. `internal/catalog/sqlserver_mapping_repository.go`
    Carga desde SQL Server el mapping `ProviderCategoryName -> RubroId`.

24. `internal/catalog/normalize.go`
    Normalización laxa usada para comparar subcategorías contra el mapping cargado desde DB.

### Imágenes, resultados, mails y logs

25. `internal/images/downloader.go`
    Descarga de imágenes y conversión a Base64.

26. `internal/results/writer.go`
    Escritura de `Resultados` y `ErroresEstructura`.

27. `internal/reporting/row_outcome_builder.go`
    Traducción de resultados exitosos y parciales a textos visibles para el Excel.

28. `internal/reporting/error_presentation.go`
    Traducción de errores técnicos a mensajes y detalles humanos para el Excel.

29. `internal/notifications/service.go`
    Lógica funcional de mails.

30. `internal/notifications/recipients.go`
    Resolución de destinatarios.

31. `internal/notifications/sendgrid.go`
    Cliente concreto de SendGrid, el servicio externo usado para enviar mails.

32. `internal/logging/logger.go`
    Logger propio, formato humano.

33. `internal/logging/factory.go`
    Construcción de ambos archivos de log con rotación vía `lumberjack`, una librería de Go para rotar archivos de log.

---

## Resumen ejecutivo del comportamiento actual

Hoy la V2 hace esto:

1. arranca el binario;
2. delega la ejecución a una CLI basada en Cobra, una librería de Go orientada a comandos de consola;
3. si se ejecuta `self-check`, valida config, carpetas, escritura y SQL Server, e informa el resultado;
4. si se ejecuta `run`, carga TOML y `.env`;
5. inicializa logging;
6. arma el runtime del batch desde `internal/app/runbatch/runtime.go`;
7. carga desde SQL Server el mapping global de categorías;
8. ejecuta el SP de providers habilitados;
9. descubre archivos `.xlsx` únicamente dentro de carpetas válidas de providers;
10. arma un `FileJob` por archivo;
11. procesa cada archivo de manera secuencial;
12. mueve el archivo a `processing`;
13. lee la primera hoja del Excel;
14. detecta el formato por cantidad de columnas;
15. valida estructura con matching laxo de headers;
16. si la estructura falla, genera `ErroresEstructura`, mueve a `processed` y notifica;
17. si la estructura es válida, mapea filas;
18. lanza un worker pool fijo por fila;
19. trata cada fila como una transacción lógica completa;
20. para archivos de 2 columnas, sincroniza stock;
21. para archivos de 19 columnas, resuelve categoría, arma payload, hace upsert y opcionalmente sincroniza imágenes;
22. junta todos los resultados;
23. los ordena por número de fila;
24. mueve el original a `processed`;
25. escribe un Excel `Resultados`;
26. manda mail con el Excel correspondiente y además el archivo procesado;
27. devuelve un resumen global del batch.

---

## Diferencia conceptual más importante contra el legacy

La V2 ya no es un servicio infinito.

### Legacy

- se disparaba con timer;
- entraba en un loop eterno;
- dormía entre iteraciones.

### V2

- hace una sola corrida;
- termina;
- deja el scheduling a un orquestador externo.

Eso vuelve al sistema:

- más simple de operar;
- más fácil de testear;
- más fácil de observar;
- más compatible con cron, scheduler, task runner o contenedor.

---

## Arranque real

Todo empieza en `main.go`.

### Responsabilidad de `main`

`main` fue dejado deliberadamente mínimo. No contiene lógica de negocio del batch, no parsea flags a mano y no hace wiring técnico.

Su secuencia es:

1. arranca el binario;
2. llama a `cli.Execute()`;
3. sale con el código de salida devuelto por la CLI.

### Rol de `internal/cli`

La capa `internal/cli` centraliza el uso de Cobra, una librería de Go pensada para CLIs con comandos, flags y help integrado.

Hoy:

1. define el comando raíz `StockCentralUploadListProductsV2`;
2. declara los flags persistentes `--settings` y `--env`;
3. registra los subcomandos `run` y `self-check`;
4. delega cada acción concreta a la capa `internal/app`;
5. si se ejecuta sin subcomando, imprime help y termina con error para evitar corridas accidentales.

### Rol de `internal/app`

El arranque técnico se reparte entre `main.go`, la CLI y la capa `internal/app`.

En la práctica:

1. `internal/app/runbatch/service.go` coordina la ejecución del caso de uso `run`;
2. `internal/app/runbatch/runtime.go` arma el runtime concreto del batch;
3. `internal/app/selfcheck/service.go` coordina el caso de uso `self-check`;
4. `internal/app/selfcheck/checks.go` implementa las verificaciones técnicas del `self-check`.

### Subcomando `run`

Cuando se elige `run`, la secuencia es:

1. crea `context.Background()`;
2. carga config con `MustLoad`;
3. inicializa logging;
4. llama a `runbatch.BuildBatch(cfg, logs)`;
5. dentro de ese runtime se abre SQL Server;
6. se cargan desde SQL Server los mappings globales de categoría usando `ProviderCategoryNameToRubroId_Get @ProviderId = 0`;
7. se construyen repositorio de providers, scanner, reader de Excel, client de productos, resolver de categorías, downloader de imágenes, mover, servicio de notificaciones y writer de resultados;
8. se construye `FileProcessor`;
9. se construye `Processor`;
10. la capa `runbatch` registra la configuración operativa con `runbatch.LogBatchBootstrap`;
11. ejecuta `runtime.Processor.Run(ctx)`;
12. si falla, registra cierre explícito de batch abortado y termina con exit code `1`;
13. si sale bien, registra el resumen final con `runbatch.LogBatchFinished`;
14. al salir, intenta cerrar los recursos abiertos por el runtime.

### Subcomando `self-check`

Cuando se elige `self-check`, el proceso no toca Excels ni corre el batch.

En cambio:

1. revisa que `appsettings.toml` exista y se pueda abrir;
2. revisa que el `.env` exista y se pueda leer;
3. intenta cargar la configuración real;
4. valida el bloque de logging;
5. revisa acceso de lectura a `input_root`;
6. prueba escritura real en `processing_root`;
7. prueba escritura real en `processed_root`;
8. prueba escritura real en la carpeta de logs;
9. intenta abrir SQL Server y hacer ping;
10. imprime una línea `OK` o `FAIL` por cada chequeo;
11. devuelve exit code `1` si alguno falla.

---

## Configuración

La configuración se divide en dos fuentes:

1. `config/appsettings.toml`
2. `config/.env`

### TOML

En `config/appsettings.toml` viven:

- datos generales de app;
- parámetros del batch;
- rutas;
- settings de base;
- settings de API;
- settings de logging;
- settings de notificaciones.

### `.env`

En `.env` viven los secretos:

- `DB_CONNECTION_STRING`
- `PRODUCTS_API_TOKEN`
- `SENDGRID_API_KEY`

### Validación al arranque

`loader.go` falla temprano si faltan datos críticos.

Valida, entre otras cosas:

- `app.name`
- `paths.input_root`
- `paths.processing_root`
- `paths.processed_root`
- `database.timeout_seconds`
- `database.providers_sp_name`
- `catalog.fallback_category_code`
- `catalog.fallback_category_name`
- `products_api.base_url`
- `DB_CONNECTION_STRING`
- `PRODUCTS_API_TOKEN`

Y además, si las notificaciones están habilitadas:

- `notifications.from_email`
- `SENDGRID_API_KEY`

### Self-check operativo

Además de la validación mínima del loader, hoy existe un subcomando `self-check`
que hace verificaciones más "de ambiente" antes de correr nada.

Ese modo confirma:

- que `appsettings.toml` exista y se pueda abrir;
- que `.env` exista y se pueda leer;
- que la configuración completa cargue;
- que la carpeta de logs esté bien configurada;
- que `input_root` se pueda leer;
- que `processing_root` se pueda crear y usar para escribir;
- que `processed_root` se pueda crear y usar para escribir;
- que la conexión a SQL Server responda.

La validación de escritura no se limita a mirar si la carpeta existe:

- intenta crear un archivo temporal;
- escribe contenido;
- cierra el archivo;
- y lo borra.

Eso da una comprobación más realista de permisos que un simple `os.Stat`.

### Parámetros operativos actuales importantes

Según el TOML actual:

- `catalog_id = 31`
- `provider_integrator_id = 3`
- `sync_images = true`
- `stop_on_file_error = false`
- `row_workers = 5`
- `row_timeout_seconds = 120`
- `category_mappings_sp_name = ProviderCategoryNameToRubroId_Get`
- `fallback_category_code = 1041`
- `fallback_category_name = Varios`

### Observación importante

`row_timeout_seconds` hoy sí gobierna un `context.WithTimeout` por fila.

Es decir:

- se carga desde configuración;
- cada worker crea un contexto propio por fila;
- y ese timeout corta llamadas de API e imágenes de esa fila sin tumbar el archivo completo.

---

## Logging

La V2 usa logger propio + `lumberjack`, una librería de Go para rotación de archivos de log.

### Archivos de log

Hay dos salidas oficiales:

1. `summary`
2. `detail`

### `summary`

- escribe a consola;
- escribe a archivo rotativo;
- está pensado para el seguimiento resumido del batch y del archivo;
- hoy también deja separadores visuales de inicio y fin de corrida;
- si `console_level` permite `DEBUG`, también puede mostrar eventos debug
  explícitos del summary, como la configuración efectiva mínima de arranque.

### `detail`

- escribe solo a archivo;
- está pensado para el seguimiento técnico y fila por fila;
- hoy además deja separadores visuales entre batchs, archivos y bloques por
  SKU para que la lectura operativa sea más clara.

### Formato

El formato es humano, no JSON.

Ejemplo:

`2026-06-16 13:40:00 INFO  file-start | provider_id=342 file=test.xlsx`

### Timestamp

Siempre usa:

`YYYY-MM-DD HH:MM:SS`

### Levels implementados

- `DEBUG`
- `INFO`
- `WARN`
- `ERROR`

### Concurrencia en logs

Cada logger tiene `sync.Mutex`, así que múltiples goroutines no mezclan texto dentro de una misma línea.

Además, el logger elige el separador de línea según el sistema operativo:

- `\r\n` cuando corre en Windows;
- `\n` cuando corre en otros sistemas.

Eso ayuda a que los archivos se lean correctamente tanto en Linux/macOS como en visores simples de Windows, por ejemplo Bloc de notas.

Además, hoy el `detail` por fila usa un buffer temporal por SKU:

- cada worker acumula en memoria las líneas de su fila;
- cuando la fila termina, escribe el bloque completo de una sola vez;
- eso evita que se intercalen eventos de SKUs distintos dentro del `detail`.
- el bloque hoy además queda rodeado por líneas en blanco para separar mejor
  visualmente una fila de la siguiente.

### Rotación y retención

Se aplica con `lumberjack`, según config:

- nombre de archivo;
- tamaño máximo;
- backups máximos;
- días máximos.

### Qué se loguea en `summary`

Ejemplos:

- arranque del batch;
- separadores explícitos `BATCH START` y `BATCH END`;
- configuración efectiva mínima de arranque cuando el summary está en `DEBUG`;
- paths;
- settings principales;
- configuración de catálogo y nombres de SP relevantes;
- cantidad de providers;
- cantidad de archivos;
- inicio y fin de archivo;
- estado final del archivo;
- envío de notificaciones;
- `WARN` y `ERROR` operativos importantes, como:
  - `file-structure-error`
  - `excel-structure-error`
  - `notification-skipped`
  - `notification-failed`
  - `file-failed`
  - `sqlserver-close-failed`
- resultado global del batch.

### Qué se loguea en `detail`

Ejemplos:

- separadores de inicio y fin de batch;
- separadores de inicio y fin de archivo;
- inicio técnico del procesamiento de archivo;
- move a `processing`;
- validación del Excel;
- mapping de filas;
- errores estructurales;
- inicio de transacción por SKU;
- validaciones;
- resolución de subcategoría;
- request JSON de producto cuando el detail está en `DEBUG`;
- upsert de producto;
- response HTTP de producto cuando el detail está en `DEBUG`, con `status` y body acotado;
- inicio, error o éxito de imágenes;
- request resumido de imagen cuando el detail está en `DEBUG`;
- response HTTP de imagen cuando el detail está en `DEBUG`, con `status` y body acotado;
- errores de API;
- errores de mail.

### Formato tipo checklist por SKU

La V2 hoy deja cada SKU como un bloque explícito y cerrado dentro del `detail`.

Ejemplo conceptual:

`-------- SKU: ABC123 ----------`

`... eventos de validación, producto e imágenes ...`

`-------- FIN SKU: ABC123 ----------`

Ese bloque:

- reúne toda la traza de una fila completa;
- se escribe junto al final de esa fila;
- no se mezcla con eventos de otros SKUs aunque haya concurrencia;
- deja mucho más simple seguir la historia completa de una transacción;
- queda visualmente separado del bloque siguiente con una línea en blanco antes
  y otra después.

---

## Obtención de providers

La V2 mantiene la idea del legacy:

- los providers válidos vienen desde SQL Server;
- no se asumen desde el filesystem.

### Conexión a SQL Server

`sqlserver.go`:

1. abre `database/sql`, el paquete estándar de Go para acceso a bases de datos, usando el driver `go-mssqldb`, que implementa conectividad con SQL Server;
2. hace `PingContext` con timeout;
3. expone `QueryContext`.

### Query usada

`sqlserver_repository.go` ejecuta:

`EXEC ProvidersGetListByEnabledAndIntegratorAndCatalogID @Enabled = @p1, @IntegratorID = @p2, @CatalogID = @p3`

con:

- `@Enabled = true`
- `@IntegratorID = cfg.Batch.ProviderIntegratorID`
- `@CatalogID = cfg.Batch.CatalogID`

### SP actual

Sale de config:

`ProvidersGetListByEnabledAndIntegratorAndCatalogID`

### Lectura del resultset

La V2 mejoró esto respecto del legacy:

- busca columnas por nombre, no por posición fija;
- exige `ID`;
- exige `Name`;
- toma `Email` solo si está presente.

### Modelo de provider en memoria

Lo que hoy se conserva del provider para el batch es:

- `ID`
- `Name`
- `Email`

### Qué aporta esto al proceso

1. `ID` filtra carpetas válidas.
2. `Name` enriquece el contexto del archivo.
3. `Email` se usa para destinatarios del mail final.

---

## Descubrimiento de archivos

El descubrimiento lo hace `scanner.go`.

### Reglas de entrada

El scanner:

1. lee el contenido inmediato de `input_root`;
2. toma solo entradas que sean directorios;
3. intenta parsear el nombre del directorio como entero;
4. conserva solo los directorios cuyo número exista en la lista de providers válidos;
5. para cada provider válido, hace `filepath.WalkDir`;
6. toma solo archivos `.xlsx`.

### Diferencia importante respecto del legacy

El legacy revisaba solo archivos directamente en la carpeta del provider.

La V2:

- filtra las carpetas raíz del provider al primer nivel;
- pero una vez adentro hace `WalkDir`, o sea recorrido recursivo.

Eso significa:

- solo considera providers válidos en carpetas numéricas;
- pero sí puede encontrar Excels en subdirectorios internos dentro de esa carpeta del provider.

### Extensión soportada

Solo:

- `.xlsx`

No:

- `.xls`

Esto elimina la inconsistencia que tenía el legacy entre extensión aceptada y reader real.

### Identidad del archivo detectado

Por cada archivo detectado se arma un `FileJob` con:

- `ProviderID`
- `ProviderName`
- `ProviderEmail`
- `InputPath`
- `RelativePath`

### Punto conceptual clave

Igual que en la decisión acordada para la V2:

- el provider operativo sale del path;
- no del contenido del archivo;
- ni del nombre del archivo.

---

## Ciclo de vida del archivo

El movimiento de archivos lo centraliza `mover.go`.

### BuildPaths

Antes de mover nada, `BuildPaths` calcula:

- `ProcessingPath`
- `ProcessedPath`
- `ResultsPath`
- `StructureErrPath`

### Reglas de rutas

Se preserva:

- la carpeta del provider;
- y la subruta relativa interna del archivo.

Ejemplo conceptual:

Si entra:

`input_root/342/sub1/sub2/catalogo.xlsx`

entonces:

- processing: `processing_root/342/sub1/sub2/catalogo.xlsx`
- processed: `processed_root/342/sub1/sub2/catalogo.xlsx`
- resultados: `processed_root/342/sub1/sub2/catalogo.result.xlsx`
- estructura: `processed_root/342/sub1/sub2/catalogo.structure-errors.xlsx`

### MoveToProcessing

1. crea la carpeta padre si hace falta;
2. mueve con `os.Rename`;
3. pisa `job.InputPath` con la nueva ubicación.

### MoveToProcessed

1. crea carpeta padre;
2. mueve con `os.Rename`;
3. vuelve a pisar `job.InputPath`.

### Consecuencia práctica

La V2 materializa claramente el estado del archivo:

- antes: en `input`;
- durante: en `processing`;
- después: en `processed`.

Esto mejora mucho la observabilidad operativa respecto del legacy.

---

## Orquestación global del batch

La corrida global vive en `processor.go`.

### Flujo de `Run`

1. crea un `BatchResult` con `StartedAt`;
2. trae providers válidos;
3. los ordena por `ID`;
4. loguea cuántos providers quedaron habilitados;
5. descubre archivos;
6. guarda `FilesDetected`;
7. procesa cada archivo secuencialmente;
8. acumula `FileResult` por archivo;
9. incrementa `FilesFailed` si un archivo falla;
10. si `stop_on_file_error = true`, corta en el primer archivo fallido;
11. si no, sigue con el resto;
12. incrementa `FilesProcessed` cuando un archivo termina sin error técnico fatal;
13. cierra `FinishedAt`.

### Concurrencia a nivel archivo

No hay concurrencia entre archivos.

Esto es deliberado:

- un archivo por vez;
- concurrencia solo dentro del archivo.

### Orden de providers

Se ordenan por `ID` para:

- tener corridas estables;
- facilitar auditoría;
- y evitar orden accidental del driver o del filesystem.

---

## Procesamiento de un archivo individual

El corazón del flujo está en `file_processor.go`.

### Flujo de `Process`

Para un `FileJob`, hace:

1. registra `startedAt`;
2. completa rutas derivadas;
3. loguea `file-start`;
4. mueve el archivo a `processing`;
5. lee el Excel;
6. si falla la estructura, entra al camino especial de `ErroresEstructura`;
7. si la estructura es válida, loguea hoja, formato y filas detectadas;
8. mapea filas;
9. resume cuántas filas son válidas, con error o vacías;
10. lanza el worker pool;
11. obtiene `RowResult` por fila;
12. mueve el original a `processed`;
13. escribe el Excel de `Resultados`;
14. consolida métricas del archivo;
15. define estado final;
16. loguea resumen;
17. intenta notificar por mail;
18. devuelve `FileResult`.

### Qué se considera falla técnica de archivo

Si falla cualquiera de estas operaciones, el archivo termina con `FileStatusFailed`:

- mover a `processing`;
- leer el Excel;
- mapear filas;
- procesar filas con error técnico global;
- mover a `processed`;
- escribir `Resultados`;
- escribir `ErroresEstructura`.

### Qué no rompe técnicamente el archivo

No rompe:

- que una o varias filas fallen;
- que haya errores parciales por imágenes;
- que SendGrid falle.

Esos casos quedan reflejados en resultados y logs, pero el archivo puede igualmente cerrar su ciclo.

---

## Estados de archivo

Los estados del archivo viven en `file_result.go`.

### Estados posibles

- `PENDING`
- `PROCESSED`
- `PROCESSED_WITH_ERRORS`
- `STRUCTURE_ERROR`
- `FAILED`

### Cómo se decide hoy

#### `STRUCTURE_ERROR`

Cuando el workbook no pasa validación estructural.

#### `PROCESSED`

Cuando se procesó el archivo y no hubo filas `ERROR` ni `PARTIAL_OK`.

#### `PROCESSED_WITH_ERRORS`

Cuando hubo:

- al menos una fila `ERROR`, o
- al menos una fila `PARTIAL_OK`.

#### `FAILED`

Cuando ocurrió un error técnico del pipeline del archivo y no se pudo completar el flujo normal.

---

## Lectura del Excel

La lectura base la hace `reader.go`.

### Librería

Usa:

- `excelize`, una librería de Go para leer y escribir archivos Excel `.xlsx`

### Reglas actuales

1. abre el archivo físico;
2. toma siempre la primera hoja;
3. lee todas las filas en memoria;
4. construye headers;
5. detecta formato por cantidad de columnas;
6. valida estructura;
7. devuelve `Workbook`.

### Si el archivo está vacío

No devuelve error técnico genérico.

Devuelve un `Workbook` con:

- `Format = UNKNOWN`
- `StructureErrors = [{ ARCHIVO, "El archivo está vacío" }]`

Eso es bueno porque transforma un problema de negocio/estructura en un artefacto auditable, no en una excepción opaca.

### Si la cantidad de columnas no coincide

Devuelve:

- `Format = UNSUPPORTED`
- error estructural indicando la cantidad detectada.

---

## Detección de formato

La detección vive en `formats.go`.

### Formatos soportados

1. `STOCK_UPDATE`
2. `FULL_IMPORT`

### Cantidades esperadas

- `2` columnas -> `STOCK_UPDATE`
- `19` columnas -> `FULL_IMPORT`

### Importante

El caso histórico de `5` columnas fue eliminado completamente del V2.

O sea:

- el formato de 5 columnas no forma parte de los formatos soportados;
- cualquier archivo con 5 columnas cae en `UNSUPPORTED`.

Eso está alineado con la decisión que tomaste de sacarlo explícitamente del alcance.

---

## Validación estructural del Excel

La validación estructural vive en `validator.go`.

### Qué valida

1. que exista header;
2. que el formato tenga una definición de columnas requeridas;
3. que todas las columnas obligatorias estén presentes;
4. que no haya duplicados relevantes.

### Mejora clave respecto del legacy

La validación no depende de texto literal rígido.

Usa normalización laxa de headers:

- trim;
- minúsculas;
- sin tildes;
- espacios colapsados.

### Ejemplos de equivalencias

Se consideran el mismo header:

- `SUB CATEGORIA`
- `sub categoría`
- ` Sub   Categoria `

### Qué devuelve si falta algo

Devuelve una lista de `StructureError`, no un string único cortado en la primera falla.

Eso permite informar:

- varias columnas faltantes;
- duplicados;
- y errores de forma en el mismo archivo.

### Columnas requeridas del formato completo

Se exigen:

- `SKU`
- `NOMBRE`
- `MARCA`
- `DESCRIPCION`
- `ALTO`
- `ANCHO`
- `LARGO`
- `PESO`
- `URL IMAGENES`
- `PRECIO`
- `IVA`
- `TIPO`
- `AHORA`
- `CATEGORIA`
- `SUB CATEGORIA`
- `STOCK`
- `OFERTA`
- `FECHA DE INICIO`
- `FECHA DE FIN`

### Diferencia importante respecto del legacy

En V2:

- `PESO` sí forma parte del contrato estructural;
- ya no queda “consumido pero no validado”.

---

## Normalización de headers y celdas

`normalize.go` separa dos ideas:

### `NormalizeHeader`

Transforma headers para comparación:

- trim
- lower
- sin tildes
- espacios múltiples colapsados

### `NormalizeCell`

Para valores de celdas hace una limpieza conservadora:

- solo trim externo

No deforma:

- descripciones;
- nombres comerciales;
- strings internos.

Esto es importante porque:

- en headers queremos flexibilidad;
- en datos queremos no tocar de más.

---

## Parsing numérico flexible

`numbers.go` implementa una mejora muy fuerte respecto del legacy.

### `ParseFlexibleFloat`

Intenta soportar:

- `1234.56`
- `1234,56`
- `1.234,56`
- `1,234.56`

### Estrategia

1. trim;
2. quitar espacios;
3. detectar el último separador como decimal probable;
4. remover miles;
5. convertir al formato que `strconv.ParseFloat` entiende.

### `ParseFlexibleInt`

Primero parsea como float, luego exige que no haya parte decimal real.

O sea:

- `10` y `10,0` son razonables;
- `10,5` falla si se esperaba entero.

### Beneficio operativo

La V2 es mucho menos frágil frente a proveedores que mezclan:

- coma decimal;
- punto decimal;
- separadores de miles;
- espacios visuales.

---

## Mapeo de filas

El mapping está en `mapper.go`.

### Qué entra

Entra un `Workbook`.

### Qué sale

Sale un slice de `MappedRow`.

Cada `MappedRow` puede contener:

- `StockUpdateRow`
- o `FullImportRow`
- o ninguna de las dos si la fila está vacía o tiene errores.

### Filosofía del mapper

El mapper:

- no llama API;
- no resuelve categorías externas;
- no mueve archivos;
- no loguea resultado final de negocio;
- no manda mails.

Su trabajo es:

- leer la fila;
- encontrar valores por nombre lógico de columna;
- validar;
- parsear;
- normalizar;
- y dejar DTO o issues.

---

## Formato de 2 columnas

### Columnas

- `SKU`
- `STOCK`

### Reglas del mapper

1. fila vacía -> `IsEmpty = true`
2. SKU vacío -> issue error
3. SKU con caracteres fuera de la whitelist acordada -> issue error
4. la whitelist de SKU permite solo ASCII alfanumérico, guion `-` y guion bajo `_`
5. si el SKU trae un carácter inválido, el `detail` informa cuál fue, por ejemplo: `Carácter inválido en SKU: "."`
6. stock inválido -> issue error
7. si no hay errores -> construye `StockUpdateRow`

### Semántica posterior

En negocio, esta fila se procesa como:

- leer producto existente;
- pisar stock;
- mandar `PUT`.

No hace:

- create;
- imágenes;
- categoría;
- importación completa.

---

## Formato completo de 19 columnas

### Campos textuales levantados

Se levantan:

- `SKU`
- `NOMBRE`
- `MARCA`
- `DESCRIPCION`
- `TIPO`
- `AHORA`
- `CATEGORIA`
- `SUB CATEGORIA`
- `FECHA DE INICIO`
- `FECHA DE FIN`

### Textos obligatorios

Se validan como requeridos:

- `SKU`
- `NOMBRE`
- `MARCA`
- `DESCRIPCION`
- `CATEGORIA`
- `SUB CATEGORIA`

### Regla adicional de `SKU`

Además del requerido:

- `SKU` solo acepta caracteres ASCII alfanuméricos, guion `-` y guion bajo `_`;
- si aparece otro carácter, la fila queda en error;
- el issue informa el primer carácter inválido detectado.

### Numéricos obligatorios

Se parsean como requeridos:

- `ALTO`
- `ANCHO`
- `LARGO`
- `PESO`
- `PRECIO`
- `IVA`
- `STOCK`

### Fechas opcionales

`FECHA DE INICIO` y `FECHA DE FIN` siguen formando parte del layout obligatorio del archivo, pero ya no se validan por contenido.

Eso significa:

- la columna debe existir en el header;
- la celda puede venir vacía;
- la celda puede venir con cualquier texto;
- ese valor se conserva como dato crudo en el DTO;
- no se parsea;
- no genera error por formato;
- no genera error por rango.

### Campo de imágenes

`URL IMAGENES` tiene una semántica especial:

- vacío no es error;
- URLs inválidas sí son error.

### OFERTA

`OFERTA` es opcional.

Si viene vacía:

- no genera error.

Si viene inválida:

- genera error de fila.

Si viene válida y es `> 0`:

- pisa `Price`;
- `ListPrice` y `NetPrice` conservan el precio base.

### IVA

Si `IVA` es mayor que `0` y menor que `1`:

- se multiplica por `100`.

O sea:

- `0.21` -> `21`

### Peso

La fila guarda:

- `WeightGrams`
- `WeightKilograms`

La regla aplicada es:

- el Excel viene en gramos;
- la API recibe kilogramos;
- entonces `kg = gramos / 1000`.

### ShortDescription

Se copia desde `NOMBRE`, igual que en el legacy.

### SyncImages

La fila queda con:

- `HasImages = len(imageURLs) > 0`
- `SyncImages = len(imageURLs) > 0`

Eso significa:

- si no trae URLs válidas, no es error;
- simplemente la fila no intentará sincronización de imágenes.

---

## Reglas de URLs de imágenes

El mapper aplica estas reglas:

1. trim global;
2. split por `&`;
3. trim por segmento;
4. descartar segmentos vacíos;
5. validar que cada URL sea HTTP o HTTPS válida;
6. si una URL es inválida, la fila entra en error.

### Consecuencia

Se replica la idea del legacy de múltiples imágenes separadas por `&`, pero con validación bastante más prolija.

---

## Fila vacía

Una fila completamente vacía:

- no es error;
- no genera DTO;
- más tarde termina como `SKIPPED`.

Además:

- no se incluye en el Excel `Resultados`.

Esto está alineado con la decisión funcional que veníamos siguiendo.

---

## Worker pool por fila

La concurrencia principal vive en `processMappedRows` dentro de `file_processor.go`.

### Cómo funciona

1. crea canal de jobs;
2. crea canal buffered de resultados;
3. lanza `rowWorkers` goroutines;
4. cada worker consume filas;
5. cada worker llama `processSingleRow`;
6. se colectan los `RowResult`;
7. al final se ordenan por `ExcelRowNumber`.

### Garantía importante

Aunque la ejecución sea concurrente, los resultados se reordenan antes de escribir el Excel final.

Eso asegura:

- orden humano legible;
- correspondencia con el Excel original;
- y estabilidad del archivo `Resultados`.

### Qué valor usa

Sale de config:

- `batch.row_workers`

Si el valor es menor que `1`, se fuerza a `1`.

### Concurrencia por imágenes

Hoy no hay subconcurrencia dentro de las imágenes de una misma fila.

Las imágenes de una fila se procesan secuencialmente.

Eso hace el comportamiento más simple y reduce mezclas en logs, a costa de no paralelizar ese subtramo.

---

## Procesamiento de una fila

`processSingleRow` trata una fila como una transacción lógica completa.

### Flujo base

1. si la fila está vacía -> `SKIPPED`;
2. si tiene errores de mapping -> `ERROR`;
3. loguea inicio por SKU;
4. según el formato:
   - stock update;
   - full import;
   - error por formato desconocido.

### Si la fila llega con errores previos

No intenta negocio.

Devuelve:

- `Status = ERROR`
- mensaje general
- detalle con concatenación de issues

Esto evita hacer requests a la API con datos ya rotos.

---

## Estado de fila

Los estados viven en `row_result.go`.

### Estados posibles

- `OK`
- `PARTIAL_OK`
- `ERROR`
- `SKIPPED`

### Significado

#### `OK`

La fila impactó correctamente el producto, y si correspondía, también las imágenes.

#### `PARTIAL_OK`

El producto quedó impactado, pero hubo algún problema en imágenes.

#### `ERROR`

La fila no pudo completarse.

Puede ser por:

- validación previa;
- error de categoría;
- error de API de producto;
- error de stock;
- etc.

#### `SKIPPED`

Fila vacía omitida sin error técnico.

---

## Caso stock update

`processStockUpdateRow` replica el caso reducido del legacy.

### Flujo

1. construye `RowResult` base;
2. marca:
   - `ProductResult = NO_PROCESADO`
   - `ImagesResult = NO_APLICA`
3. valida que exista DTO de stock;
4. llama `productsClient.SyncStockLegacy`;
5. si falla -> `ERROR`;
6. si sale bien:
   - `Status = OK`
   - `ProductResult = ACTUALIZADO`
   - `Message = Stock actualizado correctamente`
   - `Detail = El stock del producto fue actualizado correctamente.`

### Semántica heredada que conserva

`SyncStockLegacy` hace:

1. `GET` del producto;
2. pisa `Stock`;
3. `PUT` del objeto completo.

No hace create si el producto no existe.

Eso es fiel al comportamiento del legacy.

---

## Caso full import

`processFullImportRow` implementa el caso importante de 19 columnas.

### Flujo

1. arma `RowResult` base;
2. valida que exista DTO completo;
3. resuelve categoría por subcategoría;
4. arma `ProductInput`;
5. construye payload API;
6. hace upsert legacy;
7. decide si corresponde imágenes;
8. si imágenes están desactivadas globalmente -> `OK` sin imágenes;
9. si la fila no trajo URLs válidas -> `OK` sin imágenes;
10. si trae URLs válidas -> sincroniza imágenes;
11. si la categoría termina en la rama general configurada -> `PARTIAL_OK`;
12. si alguna imagen falla -> `PARTIAL_OK`;
13. si el producto quedó impactado pero imágenes se interrumpe por timeout o cancelación -> `PARTIAL_OK`;
14. si no hubo observaciones relevantes -> `OK`.

### Qué usa del DTO full import

Usa:

- SKU
- Name
- Brand
- Description
- ShortDescription
- Stock
- Price
- ListPrice
- NetPrice
- Taxes
- Height
- Width
- Depth
- WeightKilograms
- SubCategory

### Qué pasa con `CATEGORIA`

Igual que en el legacy:

- se exige y valida;
- pero la resolución final sale de `SUB CATEGORIA`.

### Qué pasa con `TIPO`, `AHORA`, fechas

Hoy se mapean y se conservan en el DTO, pero no forman parte del payload final a la API de productos.

En el caso de las fechas, antes de conservarlas:
- no se valida el formato;
- no se valida el rango;
- solo se conservan como texto crudo.

O sea:

- la V2 preserva el dato y su lectura;
- pero funcionalmente no altera la llamada a productos.

Eso sigue la realidad del legacy.

---

## Resolución de categoría / subcategoría

Esto vive en `catalog/resolver.go`.

### Orden de resolución

1. mapping precargado desde SQL Server;
2. API de subcategorías;
3. fallback configurado en TOML.

### Mapping desde SQL Server

La carga vive en `sqlserver_mapping_repository.go`.

El runtime ejecuta:

- `ProviderCategoryNameToRubroId_Get`
- con `@ProviderId = 0`

Y construye una cache en memoria:

- clave normalizada: `ProviderCategoryName`
- valor final: `products.CategoryBranch{Code: rubroId, Name: ProviderCategoryName}`

Esa cache se construye una sola vez al arrancar el batch y luego se reutiliza fila por fila.

### Diferencia menor respecto del legacy

La comparación contra el mapping cargado desde DB usa una normalización propia de `catalog/normalize.go`.

Esa normalización aplica:

- `trim`
- mayúsculas
- remoción de tildes
- colapso de espacios internos

Eso permite que el match del mapping tolere variaciones de carga como:

- `CLIMATIZACIÓN`
- `climatizacion`
- `  pequeños   electrodomésticos  `

sin alterar el valor original que vino del Excel.

La idea es conservar la flexibilidad práctica del legacy, pero sin dejar el conocimiento embebido en código.

### Fallback a API

Si no hay match en la cache de DB:

- llama `ResolveFirstSubcategory`;
- usa el valor original de `SUB CATEGORIA`, no la versión normalizada;
- que a su vez hace `GET {base_url}/subcategories/{providerID}/{texto}`;
- si la API devuelve al menos un item, toma el primero.

### Fallback final

Si tampoco resuelve por API:

- usa la rama configurada en:
  - `catalog.fallback_category_code`
  - `catalog.fallback_category_name`

### Diferencia importante respecto del legacy

En el legacy, un error en la consulta de API de subcategorías quedaba más mezclado con logging y luego terminaba en una categoría comodín.

En V2, el resolvedor también cae al fallback configurado si la API no resuelve, manteniendo el espíritu práctico del proceso histórico pero evitando dejar ese dato hardcodeado.

---

## Cliente de API de productos

El cliente base vive en `products/client.go`.

### Tecnología

Usa:

- `resty`, una librería de Go para hacer requests HTTP de forma más cómoda que con el paquete estándar

### Configuración

Se inicializa con:

- `base_url`
- `timeout_seconds`
- `PRODUCTS_API_TOKEN`
- `provider_name`

### Headers por request

Cada request agrega:

- `Authorization: <token>`
- `Content-Type: application/json`
- `Accept: application/json`

### Rutas construidas

La V2 hoy construye rutas relativas bajo el `products_api.base_url`
configurado:

- `/providers/{providerID}/products`
- `/providers/{providerID}/products/{sku}/`
- `/providers/{providerID}/products/{sku}/images/{index}`
- `/providers/{providerID}/products/{sku}/images`
- `/subcategories/{providerID}/{subcategoryName}`

Eso permite que `base_url` incluya el prefijo completo de la API. Por ejemplo:

- si `base_url = https://ctcoffice.com.ar:27443/Mp_ProductsAPI_CTC`
- la URL efectiva de producto queda `https://ctcoffice.com.ar:27443/Mp_ProductsAPI_CTC/providers/{providerID}/products`
- y la URL efectiva de subcategorías queda `https://ctcoffice.com.ar:27443/Mp_ProductsAPI_CTC/subcategories/{providerID}/{subcategoryName}`

---

## Payload de producto

El DTO de API vive en `products/dto.go`.

### Campos enviados

- `Sku`
- `ProviderId`
- `Provider`
- `Name`
- `Description`
- `ShortDescription`
- `Stock`
- `Price`
- `ListPrice`
- `NetPrice`
- `Taxes`
- `Weight`
- `Height`
- `Width`
- `Depth`
- `Active`
- `Ean`
- `Brand`
- `CategoryBranch`

### Valores fijos o heredados

- `Active = true`
- `Ean = ""`
- `Provider = cfg.ProductsAPI.ProviderName`
- `CategoryBranch` es lista con un solo elemento

### Conversión desde `ProductInput`

`BuildProductFromInput` arma el payload final tomando:

- `Weight` desde `WeightKilograms`
- `ShortDescription` ya preparada
- y la categoría ya resuelta.

---

## Upsert de producto

La lógica vive en `products/products.go`.

### Patrón

Se replica la semántica del legacy:

1. `PUT`
2. si `400 BadRequest` y body dice `Producto inexistente`
3. entonces `POST`

### Pasos

#### Update

`UpdateProduct` hace `PUT` del payload completo al SKU.

#### Create

`CreateProduct` hace `POST` a la colección del provider.

#### Detección de inexistencia

`isProductNotFound` parsea el body y busca:

- `Result.Description == "Producto inexistente"`

#### Resultado

`UpsertProductLegacy` devuelve:

- `Action = CREATE`
- o `Action = UPDATE`

### Qué considera error

Si el `PUT` no da éxito y tampoco es el caso puntual de “Producto inexistente”, se devuelve error.

Si el `POST` falla o devuelve status no 2xx, se devuelve error.

Hoy esos errores también conservan:

- el status HTTP;
- y un body truncado a un tamaño razonable para diagnóstico.

---

## Sincronización de stock

`SyncStockLegacy` replica exactamente la idea observada en el servicio original:

1. `GET` del producto;
2. pisa `Stock`;
3. `PUT` del producto completo.

Si el `GET` falla:

- la fila falla.

No:

- crea producto;
- ni hace una operación de patch parcial.

---

## Descarga y conversión de imágenes

La descarga está en `images/downloader.go`.

### Flujo

1. arma request HTTP GET con contexto;
2. descarga bytes;
3. si la respuesta no es 2xx, falla;
4. si detecta WebP por `Content-Type` o por firma binaria `RIFF....WEBP`, la convierte siempre a JPEG;
5. si no es WebP, intenta `image.Decode`;
6. si decodea bien:
   - devuelve Base64 de los bytes originales;
7. si no decodea como imagen estándar:
   - intenta `webp.Decode`, del paquete `golang.org/x/image/webp`, que agrega soporte para imágenes WebP en Go;
   - reencodea a JPEG;
   - devuelve Base64 del JPEG.

### Diferencia respecto del legacy

El legacy usaba `System.Drawing` + `Imazen.WebP`.

La V2 usa:

- decoders estándar de Go;
- más `golang.org/x/image/webp`, una librería oficial del ecosistema Go para decodificar WebP.

La intención funcional es la misma:

- tolerar imágenes estándar;
- y convertir WebP cuando haga falta.

Hoy la regla es incluso más estricta que en la primera versión de V2:

- si la imagen es WebP, se convierte a JPEG aunque Go logre decodificarla sin error;
- eso evita enviar Base64 crudo de WebP a APIs que internamente esperan formatos compatibles con `System.Drawing`.

---

## Sincronización de imágenes contra la API

La lógica vive en `products/images.go`.

### Patrón legacy respetado

1. intenta consultar imagen existente por índice;
2. si existe y el Base64 es igual, no sube;
3. si no, intenta `PUT`;
4. si el `PUT` responde `Imagen inexistente`, hace `POST`.

### Paso a paso

#### GET imagen

`GetProductImage`

Hace:

`GET {base_url}/providers/{providerID}/products/{sku}/images/{index}`

#### Comparación

Si el `GET` fue OK y el Base64 existente coincide exactamente con el nuevo:

- devuelve `Action = SKIP_SAME_IMAGE`

#### Update

Si no coincide o no pudo compararse:

- hace `PUT` al índice.

#### Create

Si el `PUT` da `400` y el body indica:

- `TransactionId == "34|Imagen inexistente"`

entonces:

- hace `POST` sobre la colección de imágenes.

### Resultado expuesto

`ImageSyncResult` informa:

- `Action`
- metadatos del `GET`
- metadatos del `PUT`
- metadatos del `POST`
- si la imagen existía o no

### Cómo impacta en el resultado de la fila

Si una o más imágenes fallan:

- la fila queda `PARTIAL_OK`

Si todas salen bien o se saltan por iguales:

- la fila puede quedar `OK`

---

## Resultado de fila y resultado de imágenes

En el Excel `Resultados`, cada fila expone:

- `Estado`
- `Producto`
- `Imagenes`
- `Mensaje`
- `Detalle`

La decisión final de esos campos visibles ya no se arma “a mano” dentro de `file_processor.go`.

Hoy existen dos capas puntuales en `internal/reporting` para separar mejor la presentación final:

- `row_outcome_builder.go` para casos exitosos o parciales;
- `error_presentation.go` para traducir errores técnicos a textos visibles al cliente.

En conjunto traducen hechos técnicos del procesamiento a:

- `Status`
- `ImagesResult`
- `Message`
- `Detail`

Eso separa mejor:

- la orquestación técnica del batch;
- de la forma final en que el resultado se presenta a negocio o cliente final.

### `ProductResult`

Hoy puede verse, por ejemplo:

- `CREADO`
- `ACTUALIZADO`
- `NO_PROCESADO`

### `ImagesResult`

Hoy puede verse:

- `OK`
- `PARCIAL`
- `NO_APLICA`

### Casos típicos

#### Producto OK sin imágenes por config

- `Status = OK`
- `ImagesResult = NO_APLICA`
- `Message = Producto creado correctamente` o `Producto actualizado correctamente`
- `Detail = No se procesaron imágenes para este producto.`

#### Producto OK sin URLs de imágenes

- `Status = OK`
- `ImagesResult = NO_APLICA`
- `Message = Producto creado correctamente` o `Producto actualizado correctamente`
- `Detail = No se procesaron imágenes para este producto.`

Esto aplica cuando `URL IMAGENES` viene vacía o con solo espacios.
Si la celda trae contenido no vacío pero inválido, la fila no cae acá:

- queda en `ERROR`;
- el mapper agrega el issue `URL de imagen inválida`;
- y la fila no llega a la etapa de sincronización de imágenes.

#### Producto con observaciones por categoría general o imágenes parciales

- `Status = PARTIAL_OK`
- `ImagesResult = PARCIAL`

`PARTIAL_OK` marca que el producto quedó impactado, pero hubo una observación importante para negocio.

Puede deberse, por ejemplo, a:

- categoría informada no reconocida y reemplazada por la categoría general configurada;
- una o más imágenes no procesadas;
- timeout o cancelación durante imágenes después de impactar el producto.

El valor realmente explicativo queda en `Detail`, donde hoy se arma con oraciones cortas, por ejemplo:

- `La categoría informada no pudo identificarse y se asignó una categoría general al producto.`
- `Se actualizaron 2 imágenes correctamente.`
- `1 imagen no pudo procesarse.`
- `El procesamiento de imágenes no pudo completarse dentro del tiempo esperado.`

#### Producto OK con imágenes ya cargadas o sin cambios

- `Status = OK`
- `ImagesResult = OK`

Ejemplos de `Detail`:

- `2 imágenes ya se encontraban cargadas.`
- `No se registraron cambios en las imágenes del producto.`

#### Error previo o de API de producto

- `Status = ERROR`

En estos casos, el Excel de resultados busca evitar mensajes técnicos crudos como:

- `status=400`
- `returned status 404`
- `context deadline exceeded`
- bodies JSON completos de la API

En cambio, hoy intenta mostrar textos humanos, por ejemplo:

- `La API rechazó la operación por los datos enviados.`
- `La API rechazó la operación: stock inválido.`
- `No se pudo descargar una imagen porque la URL informada no existe.`
- `El procesamiento de esta fila superó el tiempo máximo permitido.`

Los detalles técnicos completos siguen viviendo en logs, no en el archivo que ve el cliente final.

---

## Escritura del Excel de resultados

La escritura la hace `results/writer.go`.

### Archivo `Resultados`

Nombre:

- `<base>.result.xlsx`

Hoja:

- `Resultados`

### Columnas

- `Fila Excel`
- `SKU`
- `Estado`
- `Producto`
- `Imagenes`
- `Mensaje`
- `Detalle`

### Regla importante

Las filas `SKIPPED` no se escriben.

El archivo final queda enfocado solo en filas relevantes para negocio.

### Estilo visual

- header azul;
- autofilter;
- freeze de la primera fila;
- ajuste de anchos;
- wrap text;
- color en la columna `Estado`:
  - verde para `OK`
  - amarillo para `PARTIAL_OK`
  - rojo para `ERROR`

### Archivo `ErroresEstructura`

Nombre:

- `<base>.structure-errors.xlsx`

Hoja:

- `ErroresEstructura`

Columnas:

- `Campo`
- `Mensaje`
- `Detalle`

### Estilo visual

- header marrón/ocre;
- wrap text;
- autofilter;
- freeze;
- anchos pensados para lectura humana.

### Filosofía del archivo de salida

Es un Excel orientado a personas, no a máquinas exclusivamente.

Busca ser:

- corto;
- claro;
- una fila por SKU;
- y con detalle suficiente para entender por qué algo falló.

---

## Notificaciones por mail

La lógica funcional está en `notifications/service.go`.

### Cuándo se notifica

Hoy se notifica al final del procesamiento del archivo en dos caminos:

1. cuando terminó el flujo normal;
2. cuando terminó con `STRUCTURE_ERROR`.

### Cuándo no rompe el batch

Si SendGrid falla:

- se loguea error;
- pero el resultado del archivo no se revierte.

### Resolución de destinatarios

La hace `recipients.go`.

Regla:

1. siempre incluir `always_recipients` del config;
2. además incluir `provider.Email` si el SP lo trajo;
3. trim;
4. deduplicar case-insensitive;
5. ordenar.

### En el config actual

Siempre se incluye:

- `soporte@ctcgroup.com.ar`

Y si el provider tiene email en el resultset del SP:

- se agrega también.

### Asunto del mail

Se arma según el estado:

#### `PROCESSED`

`Archivo procesado - {providerName} - {filename}`

#### `PROCESSED_WITH_ERRORS`

`Archivo procesado con errores - {providerName} - {filename}`

#### `STRUCTURE_ERROR`

`Archivo rechazado - {providerName} - {filename}`

### Cuerpo del mail

El cuerpo es corto, como habías pedido.

No mete un resumen extenso.

Ejemplos:

- `Se proceso el archivo adjunto.`
- `Se proceso el archivo adjunto con observaciones.`
- `El archivo adjunto no pudo procesarse por estructura invalida.`

### Adjuntos

Hoy se adjuntan hasta dos archivos:

1. el Excel de salida funcional:
   - `ResultsFilePath` para `PROCESSED` y `PROCESSED_WITH_ERRORS`
   - `StructureErrorsPath` para `STRUCTURE_ERROR`
2. el archivo original ya movido a `processed`, es decir `ProcessedPath`

### Envío concreto

Lo hace `sendgrid.go`, que encapsula el uso de SendGrid, el proveedor externo de correo.

Pasos:

1. resuelve la lista completa de adjuntos;
2. lee cada archivo adjunto;
3. lo base64-encodea;
4. arma mail V3 de SendGrid;
5. agrega todos los destinatarios;
6. adjunta todos los archivos resueltos;
7. manda request HTTP;
8. falla si SendGrid responde no-2xx.

---

## Qué replica del legacy

La V2 replica explícitamente varias reglas del servicio anterior.

### 1. Providers por SP

Se sigue usando:

- `ProvidersGetListByEnabledAndIntegratorAndCatalogID`

### 2. Provider operativo por carpeta

La identidad operativa del provider sale del path válido.

### 3. Formatos de archivo realmente soportados

Se conserva:

- 2 columnas = stock update
- 19 columnas = full import

Y se elimina el formato muerto de 5 columnas.

### 4. Upsert de producto

- primero `PUT`
- luego `POST` si la API dice `Producto inexistente`

### 5. Stock update

- `GET`
- modificar stock
- `PUT` completo

### 6. Categoría resuelta desde subcategoría

No desde `CATEGORIA`.

### 7. Mapping de subcategorías precargado desde DB

Se mantiene la lógica de resolución temprana, pero ya no con un mapa embebido en el código.

### 8. Fallback a endpoint de subcategorías

También se conserva.

### 9. Fallback final de categoría por configuración

La categoría comodín sigue existiendo, pero ahora sale de config en lugar de quedar hardcodeada.

### 10. Peso

Se toma del Excel en gramos y se envía en kilogramos.

### 11. IVA

Si viene entre `0` y `1`, se transforma a porcentaje.

### 12. Oferta

Si `OFERTA > 0`, pisa `Price` y conserva `ListPrice`.

### 13. Imágenes

- split por `&`
- comparación Base64
- no resubir si es igual
- `PUT`
- `POST` si “Imagen inexistente”

---

## Qué mejora respecto del legacy

### 1. Batch one-shot

La corrida es de una sola ejecución y termina al completar el trabajo detectado.

### 2. Configuración más limpia

- TOML para no sensibles
- `.env` para secretos

### 3. Logging mucho más claro

- summary + detail
- formato humano
- rotación y retención

### 4. Validación de headers laxa

Tolera:

- mayúsculas/minúsculas
- tildes
- espacios

### 5. Parsing numérico flexible

Tolera mejor:

- coma o punto decimal;
- separadores de miles comunes;
- espacios internos;
- y también el símbolo `$` en campos numéricos como precios u oferta, siempre que el resto del valor sea válido.

### 6. Resultado formal por SKU

Ya no depende solo de logs.

### 7. Archivo de errores estructurales

El usuario final entiende por qué el archivo fue rechazado.

### 8. Concurrencia por fila

Mejora performance sin perder el concepto de una transacción por fila.

### 9. Separación de responsabilidades

Excel, DB, API, mail, logs, results y batch están bastante mejor desacoplados.

### 10. Soporte explícito de notificaciones

La V2 cierra cada archivo con una notificación formal y adjuntos entendibles.

---

## Qué diferencias funcionales concretas tiene con el legacy

### 1. Solo soporta `.xlsx`

Esto es una decisión explícita, no un accidente.

### 2. Recorre recursivamente dentro de carpetas válidas de provider

El legacy no lo hacía.

### 3. Valida `PESO` como columna obligatoria

El legacy la usaba pero no la validaba formalmente en header.

### 4. Devuelve múltiples errores estructurales juntos

El legacy fallaba en el primero.

### 5. No corta la corrida por defecto ante un archivo malo

Salvo que `stop_on_file_error` se configure en `true`.

### 6. Notifica por mail al final del archivo

Con Excel de salida y archivo procesado.

### 7. No procesa filas vacías como error

Las salta limpiamente.

---

## Flujo completo, paso a paso

Esta es la secuencia end-to-end real de la V2 hoy:

1. arranca el binario;
2. delega la ejecución a la CLI;
3. si se eligió `self-check`, ejecuta solo verificaciones técnicas y termina;
4. si se eligió `run`, carga `appsettings.toml`;
5. intenta cargar `.env`;
6. valida configuración mínima;
7. levanta `summary` y `detail`;
8. abre SQL Server y hace ping;
9. carga desde SQL Server el mapping global de categorías con `ProviderCategoryNameToRubroId_Get @ProviderId = 0`;
10. crea repositorio de providers;
11. ejecuta el SP de providers habilitados;
12. ordena providers por ID;
13. escanea `input_root`;
14. conserva solo carpetas numéricas cuyo ID exista en el resultset del SP;
15. dentro de cada provider válido, recorre recursivamente en busca de `.xlsx`;
16. arma un `FileJob` por archivo;
17. procesa cada archivo de a uno;
18. calcula rutas derivadas;
19. mueve el archivo a `processing`;
20. abre el `.xlsx` con `excelize`, una librería de Go orientada a archivos Excel;
21. toma la primera hoja;
22. detecta formato por cantidad de columnas;
23. valida estructura con matching laxo;
24. si la estructura falla:
25. mueve el original a `processed`;
26. genera `ErroresEstructura`;
27. manda mail con ese Excel y además el archivo procesado;
28. sigue al siguiente archivo;
29. si la estructura es válida:
30. mapea filas a DTOs;
31. clasifica filas vacías, válidas o con issues;
32. lanza el worker pool;
33. cada worker toma una fila;
34. abre un buffer temporal para el `detail` de ese SKU;
35. si la fila es vacía -> `SKIPPED`;
36. si la fila tiene issues -> `ERROR`;
37. si es stock update:
38. hace `GET` del producto;
39. pisa stock;
40. hace `PUT`;
41. si es full import:
42. resuelve categoría desde subcategoría;
43. primero intenta la cache cargada desde DB;
44. si no matchea, consulta la API de subcategorías;
45. si tampoco resuelve, usa el fallback configurado;
46. arma payload API;
47. loguea `product-request` en debug;
48. intenta `PUT`;
49. si la API dice `Producto inexistente`, hace `POST`;
50. loguea `product-response` en debug;
51. si imágenes globales están desactivadas, termina la fila como `OK`;
52. si la fila no trajo URLs válidas, termina la fila como `OK`;
53. si trajo URLs válidas:
54. descarga cada imagen;
55. si hace falta, convierte WebP a JPEG;
56. compara contra imagen existente;
57. si es igual, no la resube;
58. loguea `image-request` en debug;
59. si no, intenta `PUT`;
60. si la API dice `Imagen inexistente`, hace `POST`;
61. loguea `image-response` en debug;
62. si alguna imagen falla, la fila queda `PARTIAL_OK`;
63. si todas salen bien, queda `OK`;
64. escribe el bloque completo del SKU en el `detail`;
65. se colectan todos los `RowResult`;
66. se ordenan por fila de Excel;
67. se mueve el original a `processed`;
68. se genera `Resultados`;
69. se calcula estado final del archivo;
70. se manda mail con el Excel correcto y además el archivo procesado;
71. se acumula el `FileResult`;
72. termina el batch;
73. se loguea el resumen global con separadores de inicio y fin de corrida;
74. si el batch falla después de arrancar, también deja cierre explícito en logs;
75. el proceso sale.

---

## Conclusión final

`StockCentralUploadListProductsV2` es una base sólida y clara para este proceso batch.

Hoy el sistema ya resuelve de punta a punta:

- providers desde SQL Server;
- descubrimiento de archivos válidos;
- ciclo `input -> processing -> processed`;
- lectura y validación de Excel;
- normalización flexible de headers y números;
- worker pool por fila;
- tratamiento transaccional lógico de cada SKU;
- create/update de productos respetando semántica legacy;
- sincronización legacy de imágenes;
- archivo `Resultados`;
- archivo `ErroresEstructura`;
- notificación por SendGrid;
- logging resumido y detallado.

También deja bastante explícito qué cosas del negocio heredado se preservaron y cuáles se corrigieron o simplificaron.

Si lo miramos como “equivalente funcional mejorado del servicio viejo”, la V2 ya tiene resuelta la columna vertebral del proceso.

Si lo miramos como “producto terminado”, todavía hay espacio para mejoras puntuales, sobre todo en:

- timeout por fila;
- enriquecimiento del resultado técnico;
- y refinamiento fino del logging por transacción.

Pero la arquitectura base y el comportamiento esencial del proceso ya están claramente implementados y son auditables.
