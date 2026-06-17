# StockCentralUploadListProductsV2: Informe Hiper Detallado

## Objetivo de este documento

Este documento describe, con el mayor detalle posible, cómo funciona hoy el proyecto nuevo `StockCentralUploadListProductsV2`.

La idea es dejar documentado el comportamiento real implementado en Go, no solamente la intención de diseño. Por eso este informe distingue entre:

- lo que ya está funcionando en código;
- lo que replica explícitamente al legacy;
- y lo que existe como configuración o idea, pero todavía no está conectado del todo al runtime.

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
7. envía un mail corto con el adjunto de resultados;
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

1. [`go/cmd/StockCentralUploadListProductsV2/main.go`](/home/nacho/Downloads/SCUploadListProducts/go/cmd/StockCentralUploadListProductsV2/main.go)
   Punto de entrada. Solo orquesta dependencias y dispara la corrida.

2. [`go/internal/batch/processor.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/batch/processor.go)
   Orquestador principal del batch completo.

3. [`go/internal/batch/file_processor.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/batch/file_processor.go)
   Procesador de un archivo individual.

### Configuración

4. [`go/internal/config/loader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/config/loader.go)
   Carga `appsettings.toml` y `.env`, arma la configuración final y la valida.

5. [`go/internal/config/settings.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/config/settings.go)
   Modela las structs de configuración.

6. [`go/config/appsettings.toml`](/home/nacho/Downloads/SCUploadListProducts/go/config/appsettings.toml)
   Configuración no sensible.

### Providers y filesystem

7. [`go/internal/providers/sqlserver.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/providers/sqlserver.go)
   Conexión base a SQL Server.

8. [`go/internal/providers/sqlserver_repository.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/providers/sqlserver_repository.go)
   Ejecución del SP de providers y mapping del resultset.

9. [`go/internal/files/scanner.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/files/scanner.go)
   Descubrimiento de archivos en input.

10. [`go/internal/files/mover.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/files/mover.go)
    Movimiento de archivos entre `input`, `processing` y `processed`.

### Excel

11. [`go/internal/excel/reader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/reader.go)
    Apertura del `.xlsx`, lectura de la primera hoja y armado del `Workbook`.

12. [`go/internal/excel/validator.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/validator.go)
    Validación estructural del archivo.

13. [`go/internal/excel/mapper.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/mapper.go)
    Conversión fila por fila a DTOs tipados.

14. [`go/internal/excel/numbers.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/numbers.go)
    Parsing numérico flexible.

15. [`go/internal/excel/normalize.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/normalize.go)
    Normalización laxa de headers y celdas.

### API de productos y categorías

16. [`go/internal/products/client.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/client.go)
    Cliente base REST.

17. [`go/internal/products/products.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/products.go)
    Operaciones de producto y upsert legacy.

18. [`go/internal/products/images.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/images.go)
    Sincronización legacy de imágenes.

19. [`go/internal/products/subcategories.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/subcategories.go)
    Fallback REST de subcategorías.

20. [`go/internal/catalog/resolver.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/catalog/resolver.go)
    Resolución de categoría a partir de subcategoría.

21. [`go/internal/catalog/hardcoded_map.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/catalog/hardcoded_map.go)
    Mapa hardcodeado heredado del legacy.

### Imágenes, resultados, mails y logs

22. [`go/internal/images/downloader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/images/downloader.go)
    Descarga de imágenes y conversión a Base64.

23. [`go/internal/results/writer.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/results/writer.go)
    Escritura de `Resultados` y `ErroresEstructura`.

24. [`go/internal/notifications/service.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/service.go)
    Lógica funcional de mails.

25. [`go/internal/notifications/recipients.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/recipients.go)
    Resolución de destinatarios.

26. [`go/internal/notifications/sendgrid.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/sendgrid.go)
    Cliente concreto de SendGrid.

27. [`go/internal/logging/logger.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/logging/logger.go)
    Logger propio, formato humano.

28. [`go/internal/logging/factory.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/logging/factory.go)
    Construcción de ambos archivos de log con rotación vía `lumberjack`.

---

## Resumen ejecutivo del comportamiento actual

Hoy la V2 hace esto:

1. arranca;
2. carga TOML y `.env`;
3. inicializa logging;
4. abre SQL Server;
5. ejecuta el SP de providers habilitados;
6. descubre archivos `.xlsx` únicamente dentro de carpetas válidas de providers;
7. arma un `FileJob` por archivo;
8. procesa cada archivo de manera secuencial;
9. mueve el archivo a `processing`;
10. lee la primera hoja del Excel;
11. detecta el formato por cantidad de columnas;
12. valida estructura con matching laxo de headers;
13. si la estructura falla, genera `ErroresEstructura`, mueve a `processed` y notifica;
14. si la estructura es válida, mapea filas;
15. lanza un worker pool fijo por fila;
16. trata cada fila como una transacción lógica completa;
17. para archivos de 2 columnas, sincroniza stock;
18. para archivos de 19 columnas, resuelve categoría, arma payload, hace upsert y opcionalmente sincroniza imágenes;
19. junta todos los resultados;
20. los ordena por número de fila;
21. mueve el original a `processed`;
22. escribe un Excel `Resultados`;
23. manda mail con el adjunto correspondiente;
24. devuelve un resumen global del batch.

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

## Bootstrap y arranque real

Todo empieza en [`main.go`](/home/nacho/Downloads/SCUploadListProducts/go/cmd/StockCentralUploadListProductsV2/main.go).

### Responsabilidad de `main`

`main` fue dejado deliberadamente como orquestador puro. No contiene helpers locales ni lógica de negocio distribuida.

Su secuencia es:

1. crea `context.Background()`;
2. carga config con `MustLoad("config/appsettings.toml", "config/.env")`;
3. inicializa logging;
4. abre SQL Server;
5. construye repositorio de providers;
6. construye scanner;
7. construye reader de Excel;
8. construye client de API de productos;
9. construye resolver de categorías;
10. construye downloader de imágenes;
11. construye mover;
12. construye servicio de notificaciones;
13. construye writer de resultados;
14. construye `FileProcessor`;
15. construye `Processor`;
16. loguea configuración operativa;
17. ejecuta `processor.Run(ctx)`;
18. si falla, termina con exit code `1`;
19. si sale bien, loguea el resumen final.

### Qué no hace `main`

No:

- abre archivos Excel;
- ejecuta el SP directamente;
- parsea filas;
- arma requests HTTP;
- manda mails;
- ni resuelve categorías.

Todo eso queda delegado.

---

## Configuración

La configuración se divide en dos fuentes:

1. `config/appsettings.toml`
2. `config/.env`

### TOML

En [`go/config/appsettings.toml`](/home/nacho/Downloads/SCUploadListProducts/go/config/appsettings.toml) viven:

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

[`loader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/config/loader.go) falla temprano si faltan datos críticos.

Valida, entre otras cosas:

- `app.name`
- `paths.input_root`
- `paths.processing_root`
- `paths.processed_root`
- `database.timeout_seconds`
- `database.providers_sp_name`
- `products_api.base_url`
- `DB_CONNECTION_STRING`
- `PRODUCTS_API_TOKEN`

Y además, si las notificaciones están habilitadas:

- `notifications.from_email`
- `SENDGRID_API_KEY`

### Parámetros operativos actuales importantes

Según el TOML actual:

- `catalog_id = 31`
- `provider_integrator_id = 3`
- `sync_images = true`
- `stop_on_file_error = false`
- `row_workers = 5`
- `row_timeout_seconds = 120`

### Observación importante

`row_timeout_seconds` hoy sí gobierna un `context.WithTimeout` por fila.

Es decir:

- se carga desde configuración;
- cada worker crea un contexto propio por fila;
- y ese timeout corta llamadas de API e imágenes de esa fila sin tumbar el archivo completo.

---

## Logging

La V2 usa logger propio + `lumberjack`.

### Archivos de log

Hay dos salidas oficiales:

1. `summary`
2. `detail`

### `summary`

- escribe a consola;
- escribe a archivo rotativo;
- está pensado para el seguimiento resumido del batch y del archivo.

### `detail`

- escribe solo a archivo;
- está pensado para el seguimiento técnico y fila por fila.

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

Cada logger tiene `sync.Mutex`, así que múltiples goroutines no mezclan una línea con otra.

### Rotación y retención

Se aplica con `lumberjack`, según config:

- nombre de archivo;
- tamaño máximo;
- backups máximos;
- días máximos.

### Qué se loguea en `summary`

Ejemplos:

- bootstrap del batch;
- paths;
- settings principales;
- cantidad de providers;
- cantidad de archivos;
- inicio y fin de archivo;
- estado final del archivo;
- envío de notificaciones;
- resultado global del batch.

### Qué se loguea en `detail`

Ejemplos:

- inicio técnico del procesamiento de archivo;
- move a `processing`;
- validación del Excel;
- mapping de filas;
- errores estructurales;
- inicio de transacción por SKU;
- validaciones;
- resolución de subcategoría;
- upsert de producto;
- inicio, error o éxito de imágenes;
- errores de API;
- errores de mail.

### Formato tipo checklist por SKU

La V2 ya deja la marca de inicio por SKU:

`-------- SKU: ABC123 ----------`

Y a partir de ahí agrega eventos y fallas de esa transacción.

No es literalmente un bloque multi-línea rígido con una gramática fija, pero sí sigue el espíritu acordado:

- cada fila se trata como una transacción;
- se deja traza específica;
- el detalle no se mezcla conceptualmente con el resumen.

---

## Obtención de providers

La V2 mantiene la idea del legacy:

- los providers válidos vienen desde SQL Server;
- no se asumen desde el filesystem.

### Conexión a SQL Server

[`sqlserver.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/providers/sqlserver.go):

1. abre `database/sql` con driver `go-mssqldb`;
2. hace `PingContext` con timeout;
3. expone `QueryContext`.

### Query usada

[`sqlserver_repository.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/providers/sqlserver_repository.go) ejecuta:

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

El descubrimiento lo hace [`scanner.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/files/scanner.go).

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

El movimiento de archivos lo centraliza [`mover.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/files/mover.go).

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

La corrida global vive en [`processor.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/batch/processor.go).

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

El corazón del flujo está en [`file_processor.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/batch/file_processor.go).

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

Los estados del archivo viven en [`file_result.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/domain/file_result.go).

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

La lectura base la hace [`reader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/reader.go).

### Librería

Usa:

- `excelize`

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

La detección vive en [`formats.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/formats.go).

### Formatos soportados

1. `STOCK_UPDATE`
2. `FULL_IMPORT`

### Cantidades esperadas

- `2` columnas -> `STOCK_UPDATE`
- `19` columnas -> `FULL_IMPORT`

### Importante

El caso histórico de `5` columnas fue eliminado completamente del V2.

O sea:

- ya no existe como ruta funcional;
- cualquier archivo con 5 columnas cae en `UNSUPPORTED`.

Eso está alineado con la decisión que tomaste de sacarlo explícitamente del alcance.

---

## Validación estructural del Excel

La validación estructural vive en [`validator.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/validator.go).

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

[`normalize.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/normalize.go) separa dos ideas:

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

[`numbers.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/numbers.go) implementa una mejora muy fuerte respecto del legacy.

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

El mapping está en [`mapper.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/excel/mapper.go).

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
3. stock inválido -> issue error
4. si no hay errores -> construye `StockUpdateRow`

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

### Numéricos obligatorios

Se parsean como requeridos:

- `ALTO`
- `ANCHO`
- `LARGO`
- `PESO`
- `PRECIO`
- `IVA`
- `STOCK`

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

La concurrencia principal vive en `processMappedRows` dentro de [`file_processor.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/batch/file_processor.go).

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

Los estados viven en [`row_result.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/domain/row_result.go).

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
   - `Message = Actualización de stock completada`
   - `Detail = status HTTP`

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
11. si alguna imagen falla -> `PARTIAL_OK`;
12. si todas salen bien -> `OK`.

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

O sea:

- la V2 preserva el dato y su lectura;
- pero funcionalmente no altera la llamada a productos.

Eso sigue la realidad del legacy.

---

## Resolución de categoría / subcategoría

Esto vive en [`catalog/resolver.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/catalog/resolver.go).

### Orden de resolución

1. hardcode;
2. API de subcategorías;
3. fallback `Varios / 1041`.

### Hardcode

Está en [`hardcoded_map.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/catalog/hardcoded_map.go).

Conserva el conocimiento heredado del `switch` del legacy.

### Diferencia menor respecto del legacy

La normalización del hardcode se hace con:

- `strings.ToUpper`
- `strings.TrimSpace`

No usa una normalización tan rica como headers, pero sí evita problemas básicos de espacios y case.

### Fallback a API

Si no hay match hardcodeado:

- llama `ResolveFirstSubcategory`;
- que a su vez hace `GET /Mp_ProductsAPI_CTC/subcategories/{providerID}/{texto}`;
- si la API devuelve al menos un item, toma el primero.

### Fallback final

Si tampoco resuelve por API:

- usa `{ Code: "1041", Name: "Varios" }`.

### Diferencia importante respecto del legacy

En el legacy, un error en la consulta de API de subcategorías quedaba más mezclado con logging y luego terminaba en `Varios`.

En V2, el resolvedor también cae a `Varios` si la API no resuelve, manteniendo el espíritu práctico del proceso histórico.

---

## Cliente de API de productos

El cliente base vive en [`products/client.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/client.go).

### Tecnología

Usa:

- `resty`

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

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images/{index}`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images`
- `/Mp_ProductsAPI_CTC/subcategories/{providerID}/{subcategoryName}`

---

## Payload de producto

El DTO de API vive en [`products/dto.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/dto.go).

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

La lógica vive en [`products/products.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/products.go).

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

La descarga está en [`images/downloader.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/images/downloader.go).

### Flujo

1. arma request HTTP GET con contexto;
2. descarga bytes;
3. si la respuesta no es 2xx, falla;
4. intenta `image.Decode`;
5. si decodea bien:
   - devuelve Base64 de los bytes originales;
6. si no decodea como imagen estándar:
   - intenta `webp.Decode`;
   - reencodea a JPEG;
   - devuelve Base64 del JPEG.

### Diferencia respecto del legacy

El legacy usaba `System.Drawing` + `Imazen.WebP`.

La V2 usa:

- decoders estándar de Go;
- más `golang.org/x/image/webp`.

La intención funcional es la misma:

- tolerar imágenes estándar;
- y convertir WebP cuando haga falta.

---

## Sincronización de imágenes contra la API

La lógica vive en [`products/images.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/products/images.go).

### Patrón legacy respetado

1. intenta consultar imagen existente por índice;
2. si existe y el Base64 es igual, no sube;
3. si no, intenta `PUT`;
4. si el `PUT` responde `Imagen inexistente`, hace `POST`.

### Paso a paso

#### GET imagen

`GetProductImage`

Hace:

`GET /Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images/{index}`

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

#### Producto OK sin URLs válidas

- `Status = OK`
- `ImagesResult = NO_APLICA`

#### Producto OK con una o más imágenes fallidas

- `Status = PARTIAL_OK`
- `ImagesResult = PARCIAL`

#### Error previo o de API de producto

- `Status = ERROR`

---

## Escritura del Excel de resultados

La escritura la hace [`results/writer.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/results/writer.go).

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

La lógica funcional está en [`notifications/service.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/service.go).

### Cuándo se notifica

Hoy se notifica al final del procesamiento del archivo en dos caminos:

1. cuando terminó el flujo normal;
2. cuando terminó con `STRUCTURE_ERROR`.

### Cuándo no rompe el batch

Si SendGrid falla:

- se loguea error;
- pero el resultado del archivo no se revierte.

### Resolución de destinatarios

La hace [`recipients.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/recipients.go).

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

`Archivo procesado - {providerID} - {filename}`

#### `PROCESSED_WITH_ERRORS`

`Archivo procesado con errores - {providerID} - {filename}`

#### `STRUCTURE_ERROR`

`Archivo rechazado - {providerID} - {filename}`

### Cuerpo del mail

El cuerpo es corto, como habías pedido.

No mete un resumen extenso.

Ejemplos:

- `Se proceso el archivo adjunto.`
- `Se proceso el archivo adjunto con observaciones.`
- `El archivo adjunto no pudo procesarse por estructura invalida.`

### Adjunto

Se adjunta:

- `ResultsFilePath` para `PROCESSED` y `PROCESSED_WITH_ERRORS`
- `StructureErrorsPath` para `STRUCTURE_ERROR`

### Envío concreto

Lo hace [`sendgrid.go`](/home/nacho/Downloads/SCUploadListProducts/go/internal/notifications/sendgrid.go).

Pasos:

1. lee el archivo adjunto;
2. lo base64-encodea;
3. arma mail V3 de SendGrid;
4. agrega todos los destinatarios;
5. adjunta el `.xlsx`;
6. manda request HTTP;
7. falla si SendGrid responde no-2xx.

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

### 7. Hardcode de subcategorías

Se mantiene, pero externalizado en mapa.

### 8. Fallback a endpoint de subcategorías

También se conserva.

### 9. Peso

Se toma del Excel en gramos y se envía en kilogramos.

### 10. IVA

Si viene entre `0` y `1`, se transforma a porcentaje.

### 11. Oferta

Si `OFERTA > 0`, pisa `Price` y conserva `ListPrice`.

### 12. Imágenes

- split por `&`
- comparación Base64
- no resubir si es igual
- `PUT`
- `POST` si “Imagen inexistente”

---

## Qué mejora respecto del legacy

### 1. Batch one-shot

Ya no hay loop infinito ni timer extraño.

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

Tolera mejor coma/punto.

### 6. Resultado formal por SKU

Ya no depende solo de logs.

### 7. Archivo de errores estructurales

El usuario final entiende por qué el archivo fue rechazado.

### 8. Concurrencia por fila

Mejora performance sin perder el concepto de una transacción por fila.

### 9. Separación de responsabilidades

Excel, DB, API, mail, logs, results y batch están bastante mejor desacoplados.

### 10. Soporte explícito de notificaciones

Antes no existía este cierre formal por archivo.

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

Con adjunto entendible.

### 7. No procesa filas vacías como error

Las salta limpiamente.

---

## Flujo completo, paso a paso

Esta es la secuencia end-to-end real de la V2 hoy:

1. arranca el binario;
2. carga `appsettings.toml`;
3. intenta cargar `.env`;
4. valida configuración mínima;
5. levanta `summary` y `detail`;
6. abre SQL Server y hace ping;
7. crea repositorio de providers;
8. ejecuta el SP de providers habilitados;
9. ordena providers por ID;
10. escanea `input_root`;
11. conserva solo carpetas numéricas cuyo ID exista en el resultset del SP;
12. dentro de cada provider válido, recorre recursivamente en busca de `.xlsx`;
13. arma un `FileJob` por archivo;
14. procesa cada archivo de a uno;
15. calcula rutas derivadas;
16. mueve el archivo a `processing`;
17. abre el `.xlsx` con `excelize`;
18. toma la primera hoja;
19. detecta formato por cantidad de columnas;
20. valida estructura con matching laxo;
21. si la estructura falla:
22. mueve el original a `processed`;
23. genera `ErroresEstructura`;
24. manda mail con ese adjunto;
25. sigue al siguiente archivo;
26. si la estructura es válida:
27. mapea filas a DTOs;
28. clasifica filas vacías, válidas o con issues;
29. lanza el worker pool;
30. cada worker toma una fila;
31. si la fila es vacía -> `SKIPPED`;
32. si la fila tiene issues -> `ERROR`;
33. si es stock update:
34. hace `GET` del producto;
35. pisa stock;
36. hace `PUT`;
37. si es full import:
38. resuelve categoría desde subcategoría;
39. arma payload API;
40. intenta `PUT`;
41. si la API dice `Producto inexistente`, hace `POST`;
42. si imágenes globales están desactivadas, termina la fila como `OK`;
43. si la fila no trajo URLs válidas, termina la fila como `OK`;
44. si trajo URLs válidas:
45. descarga cada imagen;
46. si hace falta, convierte WebP a JPEG;
47. compara contra imagen existente;
48. si es igual, no la resube;
49. si no, intenta `PUT`;
50. si la API dice `Imagen inexistente`, hace `POST`;
51. si alguna imagen falla, la fila queda `PARTIAL_OK`;
52. si todas salen bien, queda `OK`;
53. se colectan todos los `RowResult`;
54. se ordenan por fila de Excel;
55. se mueve el original a `processed`;
56. se genera `Resultados`;
57. se calcula estado final del archivo;
58. se manda mail con el adjunto correcto;
59. se acumula el `FileResult`;
60. termina el batch;
61. se loguea el resumen global;
62. el proceso sale.

---

## Conclusión final

`StockCentralUploadListProductsV2` ya es una base bastante sólida y mucho más clara que el legacy.

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
