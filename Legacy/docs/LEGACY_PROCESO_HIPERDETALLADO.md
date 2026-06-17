# Proceso Legacy: Informe Hiper Detallado

## Objetivo de este documento

Este documento explica, de manera exhaustiva, cómo funciona el proceso legacy de `StockCentralUploadListProducts`, enfocado en el flujo real que toma archivos Excel desde disco, los procesa línea por línea y luego interactúa con la API de productos.

El objetivo no es describir todo el repositorio histórico, sino dejar documentado el comportamiento concreto del servicio que hoy importa productos desde Excel hacia la API de productos.

## Proyectos involucrados en el flujo

El proceso real está repartido entre tres proyectos:

1. [`StockCentralUploadListProducts/Program.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/Program.cs)
   Punto de entrada del Windows Service.

2. [`StockCentralUploadListProducts/MainServices.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/MainServices.cs)
   Orquesta la búsqueda de archivos, la carga de configuración, la obtención de providers y el disparo de la importación.

3. [`StockCentralToMagento/Business/ProcessSystem.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralToMagento/Business/ProcessSystem.cs)
   Expone helpers para obtener archivos y delega la importación al conector.

4. [`MG2Connector/Magento.cs`](/home/nacho/Downloads/SCUploadListProducts/MG2Connector/Magento.cs)
   Contiene la lógica principal de lectura del Excel y las llamadas HTTP a la API de productos.

5. [`StockCentralToMagento/DataAccess/ProviderDALC.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralToMagento/DataAccess/ProviderDALC.cs)
   Obtiene desde base de datos la lista de providers habilitados para procesar.

6. [`MG2Connector/LogSystem.cs`](/home/nacho/Downloads/SCUploadListProducts/MG2Connector/LogSystem.cs)
   Maneja el logging legacy.

7. [`StockCentralUploadListProducts/App.config`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/App.config)
   Contiene la configuración del servicio y de la API.

---

## Resumen ejecutivo del proceso

El servicio legacy:

1. Arranca como Windows Service.
2. Espera un breve intervalo inicial.
3. Entra en un loop infinito.
4. Consulta en base de datos qué providers debe considerar.
5. Para cada provider, busca archivos dentro de una carpeta cuyo nombre coincide con el `providerID`.
6. Si encuentra archivos Excel, procesa cada archivo completo.
7. Dentro de cada archivo, procesa las filas una por una.
8. Según la cantidad de columnas, decide qué tipo de importación hacer.
9. Para el formato “completo”, arma un payload de producto y hace `PUT` a la API.
10. Si el producto no existe, hace `POST` para crearlo.
11. Si está habilitada la sincronización de imágenes, descarga cada imagen remota y la sube a la API.
12. Cuando termina de procesar el archivo, lo mueve a una carpeta histórica.
13. Si el archivo no es Excel, lo mueve a otra carpeta de descartes o “file not found”.

Eso es el corazón del proceso.

---

## Arranque del servicio

### Entry point

El punto de entrada es [`StockCentralUploadListProducts/Program.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/Program.cs), que simplemente levanta la clase `MainServices`.

### OnStart

En [`MainServices.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/MainServices.cs), el método `OnStart` hace lo siguiente:

1. Crea un `Timer`.
2. Configura la connection string global para acceso a SQL Server.
3. Define un intervalo inicial de `30000` ms.
4. Configura `AutoReset = false`.
5. Se suscribe al evento `Elapsed`.
6. Intenta leer `SincronizarImagenes` desde config.
7. Intenta leer `LogType` y configurar `LogSystem`.
8. Escribe un log de startup.
9. Inicia el timer.

### Observación importante

Aunque existe un timer, el proceso real no funciona como un “timer periódico limpio”. Cuando se dispara el evento, entra en un `while (true)` infinito y ya no vuelve al modelo de timer tradicional.

En otras palabras:

- el timer sirve solo para disparar la primera ejecución;
- después el proceso queda corriendo en loop permanente;
- entre iteraciones hace `Thread.Sleep(timeZ)`.

Esto es importante porque el servicio legacy no es un batch de una sola pasada. Es un daemon/service continuo.

---

## Configuración relevante

La configuración vive en [`App.config`](/home/nacho/Downloads/SCUploadListProducts/StockCentralUploadListProducts/App.config).

### Claves directamente relevantes para este flujo

- `timeX`
- `timeZ`
- `PathLog`
- `LogType`
- `MagentoAdmin`
- `MagentoPass`
- `StockCentral`
- `SincronizarImagenes`
- `ActivarSoloCatalogoNro`
- `URL_API_CTC`
- `ProviderIntegrator`
- `TokenProviderIntegrator`
- `StorePathUploadProducts`
- `IFPS_HistoryDirectory`
- `IFPS_FileNotFoundDirectory`

### Qué hace cada una

- `timeZ`
  Es la espera entre iteraciones completas del loop.

- `timeX`
  Está configurada y comentada como espera para nueva búsqueda, pero en este flujo concreto no tiene un rol real visible en la ejecución principal mostrada.

- `PathLog`
  Base donde se escriben los archivos de log.

- `LogType`
  Define cómo loguea `LogSystem`.

- `MagentoAdmin` y `MagentoPass`
  Se leen al inicio del loop, pero en este flujo puntual la autenticación Magento tradicional está prácticamente desactivada, porque la llamada `ProcessSystem.GetToken(User, pass)` está comentada.

- `SincronizarImagenes`
  Activa o desactiva la carga de imágenes.

- `ActivarSoloCatalogoNro`
  Filtra qué catálogo considerar al obtener providers desde DB.

- `URL_API_CTC`
  Base URL de la API REST de productos.

- `ProviderIntegrator`
  Valor que se envía dentro del payload como nombre del proveedor integrador. En el config actual es `CTC`.

- `TokenProviderIntegrator`
  Token usado para autenticar contra la API de productos.

- `StorePathUploadProducts`
  Carpeta raíz donde busca archivos a procesar.

- `IFPS_HistoryDirectory`
  Carpeta destino para archivos procesados.

- `IFPS_FileNotFoundDirectory`
  Carpeta destino para archivos descartados por no ser Excel o por nombre inválido.

### Configuraciones presentes pero no centrales para este flujo

Hay otras claves en el `App.config` vinculadas a SFTP, Magento clásico u otros procesos históricos. No son el centro del flujo de importación Excel -> API de productos que estamos documentando acá.

---

## Modelo de ejecución general

### Paso 1: se dispara el evento del timer

`timerStore_Elapsed`:

1. Detiene el timer.
2. Loguea el inicio del proceso.
3. Lee usuario y password desde config.
4. Si falla, usa fallback hardcodeado:
   - usuario: `admin`
   - password: `ctc2018`
5. Entra en `while (true)`.
6. Dentro del loop, llama a `Process(user, pass)`.
7. Si `Process` falla, loguea error grande y espera 15 segundos.
8. Al final de cada vuelta, duerme `timeZ`.

### Observación fuerte

El `timerStore.Start()` que aparece después del `while (true)` es, en la práctica, inalcanzable.

Eso confirma que el servicio real es:

- un proceso continuo,
- serial,
- de polling periódico,
- sin cierre natural por iteración.

---

## Obtención de providers

Dentro de `Process`, el servicio obtiene la lista de providers desde SQL Server.

### Query usada

Se llama a:

- método: `ProviderDALC.GetListByEnabledAndIntegratorAndCatalogID(true, 3, SolounCatalogo)`
- archivo: [`ProviderDALC.cs`](/home/nacho/Downloads/SCUploadListProducts/StockCentralToMagento/DataAccess/ProviderDALC.cs)
- stored procedure: `ProvidersGetListByEnabledAndIntegratorAndCatalogID`

### Parámetros enviados al SP

- `@Enabled = true`
- `@IntegratorID = 3`
- `@CatalogID = SolounCatalogo`

### Qué implica esto

El servicio no recorre providers “porque sí” ni porque existan carpetas. Primero consulta la base, obtiene una lista válida de providers y recién después inspecciona carpetas.

O sea:

- la base decide qué providers están habilitados;
- el filesystem solo se revisa para esos providers.

### Observación adicional

En el código también se carga `CatalogDALC.GetCatalogMagento()`, pero en este flujo puntual ese resultado no se usa después de obtenerlo. Queda como residuo o soporte histórico de otras lógicas.

---

## Cómo decide qué carpetas revisar

Para cada provider obtenido desde DB:

1. Toma `StorePathUploadProducts`.
2. Concatena `\\` + `pr.ID`.
3. Busca archivos dentro de esa carpeta.

Ejemplo conceptual:

`<StorePathUploadProducts>\<ProviderID>`

### Importante: no recorre recursivamente

El helper [`ProcessSystem.GetFileInfoListToProcess`](/home/nacho/Downloads/SCUploadListProducts/StockCentralToMagento/Business/ProcessSystem.cs) hace:

- `new DirectoryInfo(storeDirectory)`
- `GetFiles().ToList()`

Eso significa:

- revisa solo archivos del directorio inmediato;
- no baja a subdirectorios;
- no hace búsqueda recursiva.

### Conclusión

El legacy espera una estructura simple:

- una carpeta raíz;
- una subcarpeta por `providerID`;
- archivos directamente adentro de esa subcarpeta.

---

## Qué archivos intenta procesar

La validación de tipo de archivo se hace con `isExcel(FileInfo file)`.

Acepta extensiones:

- `.xls`
- `.xlsx`

### Pero hay una inconsistencia importante

Aunque la validación superficial acepta `.xls`, la lectura real del archivo se hace con:

- `ExcelReaderFactory.CreateOpenXmlReader(stream)`

Ese reader es para formato OpenXML, es decir `.xlsx`.

### Implicancia real

El servicio “declara” aceptar `.xls`, pero el flujo principal parece estar preparado realmente para `.xlsx`.

Eso puede generar este comportamiento:

- el wrapper lo acepta como Excel;
- luego la lectura real puede fallar si el archivo es `.xls` antiguo.

Esta es una inconsistencia concreta del legacy.

---

## Qué hace con los archivos encontrados

Por cada archivo encontrado en la carpeta del provider:

1. Calcula un nombre histórico usando `FormatFileName`.
2. Intenta parsear metadatos del nombre con `GetFileNameComponets`.
3. Fuerza el `ProveedorID` al `pr.ID` del loop actual.
4. Si es Excel, lo importa.
5. Luego lo mueve a `IFPS_HistoryDirectory`.
6. Si no es Excel, lo mueve a `IFPS_FileNotFoundDirectory`.

### Detalle importante sobre el nombre del archivo

Existe una convención histórica:

`Nombre_YYYY-MM-DD HHmmss_ProveedorID.xlsx`

Sin embargo, en este proceso puntual:

- el provider efectivo no sale del nombre del archivo;
- sale del provider que se está iterando en ese momento.

Es decir, el nombre del archivo se parsea, pero la importación usa `pr.ID`.

### Qué agrega `FormatFileName`

Antes de moverlo, agrega timestamp actual al nombre:

`<nombre original> yyyy-MM-dd HHmmss<extensión>`

### Comportamiento importante

Si el archivo es Excel, se lo mueve a histórico después de llamar a la importación, sin verificar en profundidad si todas las filas fueron exitosas.

O sea:

- “archivo procesado” no significa “todo salió bien”;
- significa más bien “se intentó procesar y se terminó la rutina”.

---

## Método que dispara la importación

La importación de cada archivo se inicia desde:

- [`ProcessSystem.ImportProductsfromExcel`](/home/nacho/Downloads/SCUploadListProducts/StockCentralToMagento/Business/ProcessSystem.cs)

Ese método:

1. Crea una instancia de `Magento`.
2. Llama a `ImportProductfromFileLine(Path, ProveedorID)`.
3. Loguea el resultado textual devuelto.

La lógica pesada no está ahí. Está en `MG2Connector/Magento.cs`.

---

## Apertura del Excel

El método central es:

- `Magento.ImportProductfromFileLine(string Path, string ProviderID)`

### Qué hace al inicio

1. Loguea “Iniciando Importación”.
2. Abre el archivo con `File.Open`.
3. Usa `CreateOpenXmlReader(stream)`.
4. Convierte a `DataSet` con `AsDataSet()`.
5. Lee `URL_API_CTC`.
6. Lee `TokenProviderIntegrator`.
7. Crea un cliente `Magento m_api = new Magento(URL_API_CTC)`.

### Cómo recorre el Excel

Hace:

`for (int j = 1; j < result.Tables[0].Rows.Count; j++)`

Eso significa:

- asume que la fila 0 es cabecera;
- procesa desde la fila 1 en adelante;
- no filtra filas vacías de forma explícita antes del loop.

---

## Modos de procesamiento según cantidad de columnas

La lógica pivota sobre:

`switch (result.Tables[0].Columns.Count)`

Hay tres escenarios codificados:

1. `case 2`
2. `case 5`
3. `default`

### Caso 2 columnas

Es un modo reducido para actualización de stock.

#### Estructura esperada

- columna 0: `SKU`
- columna 1: `STOCK`

#### Flujo

1. Lee SKU.
2. Lee stock.
3. Hace `GET /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/`
4. Si la API responde `200 OK`:
   - parsea el producto existente;
   - reemplaza `product.Stock`;
   - serializa el producto entero;
   - hace `PUT /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/`
5. Si el `GET` no responde OK:
   - no crea producto;
   - no hay fallback de alta;
   - simplemente no actualiza nada.

#### Conclusión

Con 2 columnas:

- solo actualiza stock;
- solo si el producto ya existe;
- no hace alta;
- no hace imágenes;
- no arma payload completo desde Excel.

### Caso 5 columnas

Existe `case 5`, pero no tiene implementación.

Hace simplemente:

- `break;`

Conclusión:

- el caso de 5 columnas está efectivamente muerto o desactivado;
- no produce procesamiento real.

### Caso default

Este es el caso importante. Es el formato “completo”.

En la práctica, el método espera acá el layout largo del Excel, de 19 columnas.

---

## Validación estructural del Excel completo

Antes de procesar la fila, el método valida la cabecera leyendo `result.Tables[0].Rows[0][x]`.

### Columnas validadas por nombre exacto

Se exige, en mayúsculas exactas:

0. `SKU`
1. `NOMBRE`
2. `MARCA`
3. `DESCRIPCION`
4. `ALTO`
5. `ANCHO`
6. `LARGO`
7. `PESO` no se valida por nombre
8. `URL IMAGENES`
9. `PRECIO`
10. `IVA`
11. `TIPO`
12. `AHORA`
13. `CATEGORIA`
14. `SUB CATEGORIA`
15. `STOCK`
16. `OFERTA`
17. `FECHA DE INICIO`
18. `FECHA DE FIN`

### Particularidad muy importante sobre el peso

La validación del header de peso está comentada:

- no exige formalmente `PESO MILIGRAMOS`;
- pero después sí consume el valor de la columna 7.

Eso significa:

- la columna 7 funcionalmente existe y se usa;
- pero su nombre no está siendo validado en runtime.

### Cómo falla si la estructura no coincide

Si cualquier header validado no coincide exactamente, el método hace `return` inmediato con un string tipo:

`-1| ... Error de formato en <CAMPO>|`

### Implicancias

1. La validación es rígida.
2. Es sensible a mayúsculas, tildes y espacios porque usa `ToUpper()` comparado contra textos fijos.
3. No informa varias faltas juntas.
4. Falla en la primera diferencia encontrada.
5. Devuelve error textual, no un objeto estructurado.

---

## Significado real de las columnas

### Mapeo funcional

0. `SKU`
   Identificador del producto.

1. `NOMBRE`
   Va a `Name` y también a `ShortDescription`.

2. `MARCA`
   Va a `Brand`.

3. `DESCRIPCION`
   Va a `Description`.

4. `ALTO`
   Va a `Height`.

5. `ANCHO`
   Va a `Width`.

6. `LARGO`
   Va a `Depth`.

7. `PESO`
   Se usa para `Weight`.

8. `URL IMAGENES`
   Se separa por `&` para obtener múltiples URLs.

9. `PRECIO`
   Se usa como `Price`, `ListPrice` y `NetPrice` base.

10. `IVA`
   Va a `Taxes`.

11. `TIPO`
   Se valida estructuralmente, pero no tiene impacto visible en el payload final de este flujo.

12. `AHORA`
   Se valida estructuralmente, pero no tiene impacto visible en el payload final de este flujo.

13. `CATEGORIA`
   Se valida estructuralmente, pero no se usa para resolver categoría enviada a la API.

14. `SUB CATEGORIA`
   Esta sí es la columna que dispara la resolución de categoría/subcategoría a enviar.

15. `STOCK`
   Va a `Stock`.

16. `OFERTA`
   Si tiene valor positivo, reemplaza `Price`.

17. `FECHA DE INICIO`
   Se valida; no se observa uso funcional real en el payload.

18. `FECHA DE FIN`
   Se valida; no se observa uso funcional real en el payload.

### Conclusión importante

No todas las columnas validadas son usadas realmente.

En particular:

- `TIPO`
- `AHORA`
- `CATEGORIA`
- `FECHA DE INICIO`
- `FECHA DE FIN`

aparecen como parte del contrato estructural, pero no tienen efecto visible en el payload que se manda a la API de productos en este flujo.

---

## Resolución de categoría y subcategoría

Este es uno de los puntos más importantes del legacy.

### Regla principal

El servicio no usa la columna `CATEGORIA` para resolver la categoría a enviar.

La lógica se basa sobre la columna:

- `SUB CATEGORIA` = índice 14

### Mapeo hardcodeado

Primero intenta resolver por un `switch` hardcodeado:

- `ACCESORIOS CEL` -> `1217`
- `AUDIO` -> `1211`
- `CELULARES` -> `1217`
- `CLIMATIZACIÓN` -> `1215`
- `COMPUTACIÓN` -> `1213`
- `GAMING` -> `1214`
- `ILUMINACIÓN` -> `1249`
- `LINEA BLANCA` -> `1215`
- `MOVILIDAD` -> `1226`
- `PEQUEÑOS ELECTRODOMESTICOS` -> `1212`
- `SALUD` -> `1234`
- `TV` -> `1214`
- `Maquillaje y Skincare` -> `1235`
- `Pequeños Electro` -> `1212`
- `Cocina` -> `1246`
- `Herramientas` -> `1248`
- `Accesorios Niños` -> `1218`
- `Juegos y Juguetes` -> `1220`
- `Outdoors` -> `1226`
- `Accesorios de Viajes` -> `1221`
- `Accesorios Mascotas` -> `1237`

Si no encuentra match:

- usa fallback `Code = 1041`
- `Name = Varios`

### Consulta a la API para categoría no resuelta

Si cae en el fallback `1041`, intenta buscar una parametrización externa en la API:

`GET /Mp_ProductsAPI_CTC/subcategories/{ProviderID}/{SubcategoriaTexto}`

Si la respuesta es `200 OK` y trae datos:

- toma `pr[0].ID`
- toma `pr[0].Name`
- pisa el `CategoryBranch` local con esos valores.

### Qué significa esto realmente

La resolución de categoría tiene dos niveles:

1. hardcode local en código;
2. fallback a consulta REST por provider + nombre de subcategoría.

### Conclusión clave

La “categoría” enviada al producto termina saliendo de una estructura `CategoryBranch`, armada a partir de:

- un hardcode por `SUB CATEGORIA`, o
- una búsqueda REST de subcategoría parametrizada.

La columna `CATEGORIA` del Excel no gobierna esta decisión.

---

## Conversión de campos numéricos

### Dimensiones

Se hace:

- `Height = Round(col4, 2)`
- `Width = Round(col5, 2)`
- `Depth = Round(col6, 2)`

Usa:

- `Convert.ToDecimal(...)`
- `Math.Round(..., 2, MidpointRounding.AwayFromZero)`

### Peso

Se hace:

- `Weight = Round(col7, 2) / 1000`

### Interpretación del peso

El código divide por `1000`, por lo que:

- si el valor fuente está en gramos, el payload sale en kilos;
- si el valor fuente estuviera en miligramos, la conversión sería incorrecta para kg.

Lo que importa acá es documentar el comportamiento real:

- toma la columna 7;
- la redondea a 2 decimales;
- divide por `1000`.

### Precio e impuestos

- `Price = col9`
- `ListPrice = col9`
- `NetPrice = col9`
- `Taxes = col10`

Luego:

- si `Taxes > 0 && Taxes < 1`, multiplica por `100`

Esto intenta tolerar que el IVA venga como:

- `0.21`
- o `21`

### Stock

`Stock = Convert.ToInt32(col15)`

### Oferta

Intenta convertir `col16` a decimal.

Si `oferta > 0`:

- loguea “CON OFERTA”
- pisa `Price = oferta`

Si no:

- deja `Price` igual al precio base
- `ListPrice` y `NetPrice` quedan en el valor original del Excel

### Implicancia comercial

Cuando hay oferta:

- `Price` cambia;
- `ListPrice` no cambia;
- `NetPrice` no cambia.

Eso parece querer conservar:

- precio publicado actual en `Price`;
- precio de lista/original en `ListPrice`.

---

## Armado del payload de producto

El objeto enviado a la API es `ProductApi`.

### Campos que se cargan

- `Sku`
- `ProviderId`
- `Provider`
- `Stock`
- `Name`
- `Description`
- `ShortDescription`
- `Price`
- `ListPrice`
- `NetPrice`
- `Taxes`
- `Height`
- `Width`
- `Depth`
- `Weight`
- `Active = true`
- `Ean = ""`
- `Brand`
- `CategoryBranch = LCB`

### Valores relevantes

- `ProviderId` sale del provider actual.
- `Provider` sale de config, no del Excel.
- `ShortDescription` se iguala a `Name`.
- `Ean` va vacío.
- `CategoryBranch` es una lista con un único elemento.

### Qué no manda

No se ve que mande en este flujo:

- fechas de oferta,
- campo `TIPO`,
- campo `AHORA`,
- categoría textual original,
- subcategoría textual original como texto libre.

Solo manda la categoría ya resuelta en `CategoryBranch`.

---

## Estrategia de alta / actualización de producto

El patrón es de tipo “upsert manual”.

### Paso 1: intenta actualizar

Hace:

`PUT /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/`

con el JSON completo del producto.

### Paso 2: analiza el error si falla

Si el `PUT` responde `400 BadRequest`:

1. Parsea el body.
2. Loguea la respuesta.
3. Si `Result.Description == "Producto inexistente"`:
   - hace `POST /Mp_ProductsAPI_CTC/providers/{ProviderID}/products`
   - manda el mismo JSON

### Qué implica esto

La lógica real es:

- primero intenta update;
- si la API dice específicamente que el producto no existe, lo crea.

### Qué no hace

No hay:

- validación sofisticada de otros códigos HTTP;
- retry;
- control de idempotencia formal;
- rollback;
- diferenciación clara entre errores transitorios y permanentes.

Es una estrategia simple:

- `PUT`
- si “producto inexistente” -> `POST`

---

## Lógica de imágenes

Las imágenes se procesan solo si la config `SincronizarImagenes` vale `"1"`.

### Fuente de imágenes

Se toma el contenido de la columna `URL IMAGENES` y se parte por `&`.

Eso significa que el Excel puede traer múltiples URLs concatenadas con `&`.

### Flujo por cada imagen

Para cada URL:

1. Loguea que va a subir la imagen.
2. Descarga la URL con `WebClient.DownloadData`.
3. Fuerza TLS 1.1 / 1.2.
4. Convierte bytes a Base64.
5. Intenta abrir la imagen con `Image.FromStream`.
6. Si falla, asume que puede ser WebP:
   - carga `Imazen.WebP`
   - decodifica
   - regraba como JPEG
   - vuelve a convertir a Base64

### Detección de imagen ya existente

Antes de subirla, hace:

`GET /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/images/{i}`

Si responde `200 OK`:

1. parsea `Result.Base64`
2. compara con el Base64 recién generado
3. si son iguales:
   - no actualiza
   - loguea que no se sube porque es igual

### Update / create de imagen

Si la imagen debe actualizarse:

1. hace `PUT /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/images/{i}`
2. si responde `400 BadRequest`
3. y si `TransactionId == "34|Imagen inexistente"`
4. hace `POST /Mp_ProductsAPI_CTC/providers/{ProviderID}/products/{sku}/images`

### Conclusión

La estrategia de imágenes es:

1. descargar imagen remota;
2. normalizar formato si hace falta;
3. comparar contra imagen existente;
4. evitar actualización si son iguales;
5. `PUT` por índice;
6. `POST` si la imagen no existía.

### Observaciones importantes

1. La comparación es por Base64 completo.
2. Puede ser costosa en CPU y memoria.
3. Los errores de imagen se capturan por imagen y no frenan el producto completo.
4. Si una imagen falla, el producto puede igual haberse actualizado.

---

## Qué pasa con errores por fila

Dentro del `default`, cada fila está envuelta en un `try/catch`.

Si algo falla en la fila:

- loguea el error;
- sigue con la siguiente fila.

Eso vuelve al proceso tolerante a fallas parciales.

### Qué significa esto operativamente

Un archivo puede terminar con:

- algunas filas bien;
- algunas filas mal;
- algunas con producto actualizado pero imágenes fallidas;
- algunas sin impacto por error de parseo o API.

Y aun así el archivo entero será movido a histórico al final del proceso exterior.

---

## Qué pasa con errores globales

Hay varios niveles de captura:

### Nivel fila

Errores del producto individual dentro del `default`.

### Nivel importación de archivo

El `try/catch` principal de `ImportProductfromFileLine` captura errores graves y devuelve:

`-1| EERROR GRAVE ...`

### Nivel proceso del servicio

`Process` no hace demasiada inteligencia sobre el resultado textual de la importación. Solo dispara el método.

### Nivel loop de servicio

`timerStore_Elapsed` captura excepciones grandes de `Process`, loguea y reintenta luego de una pausa.

### Conclusión

La resiliencia del legacy existe, pero es bastante artesanal:

- mucho `try/catch`;
- mucho log textual;
- poco modelado formal de estados;
- poco contrato fuerte entre capas.

---

## Logging legacy

El logging vive en [`MG2Connector/LogSystem.cs`](/home/nacho/Downloads/SCUploadListProducts/MG2Connector/LogSystem.cs).

### Modos

`LogType` puede operar así:

- `0`: solo excepciones o logging más limitado, según uso histórico.
- `1`: escribe directamente a disco en cada llamada.
- `2`: acumula en memoria y escribe por bloque al cerrar transacción.

El comentario del código dice:

- `0`: solo excepciones
- `1`: graba en disco por cada `WriteLog`
- `2`: graba una sola vez por cada TRN

### Archivos de log

Se generan archivos como:

- `yyyy-MM-dd_StockCentralUploadListProducts_<n>.log`
- `yyyy-MM-dd_ImportFile_<n>.log`
- `yyyy-MM-dd_MG2Connector_<n>.log`

### Formato de línea

Usa timestamp del estilo:

`yyyy/MM/dd HH:mm:ss:fff`

### Comportamiento particular

Cada método intenta abrir archivos con sufijo incremental `_0`, `_1`, `_2`, etc., hasta `_32`, por si alguno está bloqueado.

### Qué loguea en este flujo

Hay al menos dos niveles visibles:

- un log general del servicio;
- un log de importación línea por línea en `ImportFile`.

### Limitaciones

1. El logging no está centralizado por contexto de archivo o SKU.
2. No hay estructura formal de eventos.
3. No hay separación limpia entre resumen y detalle.
4. El formato es plano y muy textual.
5. No hay correlación fuerte entre archivo, SKU y request salvo por texto.

---

## Qué hace realmente con la columna CATEGORIA

Esto merece una aclaración explícita porque suele generar confusión.

### Respuesta corta

La columna `CATEGORIA`:

- se valida como parte de la estructura;
- pero no define la categoría enviada a la API.

### Lo que sí manda la categoría

La resolución sale de:

- `SUB CATEGORIA` hardcodeada;
- o fallback a endpoint de subcategorías por provider.

### Conclusión

En este flujo legacy, `CATEGORIA` funciona más como parte del contrato del Excel que como dato de negocio realmente usado.

---

## Qué hace realmente con las fechas

Las columnas:

- `FECHA DE INICIO`
- `FECHA DE FIN`

son validadas en la cabecera, y se declaran variables `fechaInicio` y `fechaFin`, pero en este flujo no se observa que se integren al payload ni que alteren la lógica final del producto.

Conclusión:

- estructuralmente se exigen;
- funcionalmente no gobiernan el resultado visible del request.

---

## Qué hace realmente con AHORA y TIPO

Pasa algo parecido:

- `TIPO` y `AHORA` se exigen en el header;
- pero no aparecen después como campos decisivos en el request final.

Eso sugiere:

- contrato heredado del layout;
- uso histórico anterior o esperado;
- pero sin efecto real actual en esta rutina específica.

---

## Qué no hace el legacy

Es igual de importante dejar claro lo que el servicio no hace.

### No hace procesamiento recursivo de carpetas

Solo revisa archivos directamente dentro de la carpeta del provider.

### No hace batch de una sola pasada

Corre indefinidamente.

### No hace concurrencia por archivo ni por fila

Todo el flujo es secuencial.

### No genera un archivo de resultado por SKU

Solo deja logs.

### No envía emails

No hay notificación al final del archivo en este flujo.

### No distingue claramente estados de archivo

No hay un modelo formal de:

- éxito total,
- éxito parcial,
- error de estructura,
- error parcial por SKU.

### No valida de manera laxa

La validación de headers es rígida y literal.

### No soporta bien formatos numéricos flexibles

Usa conversiones directas `Convert.ToDecimal` y `Convert.ToInt32`, sin una capa de normalización más robusta.

### No tiene separación fuerte entre dominio, transporte y orquestación

La lógica está bastante mezclada entre lectura, parsing, reglas de negocio, integración HTTP y logging.

---

## Flujo completo, paso a paso

Esta es la secuencia end-to-end real:

1. Arranca el Windows Service.
2. Se inicializa logging y configuración.
3. Se dispara el timer inicial.
4. El timer entra en un loop infinito.
5. Cada vuelta del loop llama a `Process`.
6. `Process` lee paths y parámetros.
7. `Process` consulta providers habilitados por SP.
8. Para cada provider:
9. Arma la ruta `<StorePathUploadProducts>\<providerID>`.
10. Lista archivos de esa carpeta.
11. Por cada archivo:
12. Si no es Excel, lo mueve a `IFPS_FileNotFoundDirectory`.
13. Si es Excel, llama a `ImportProductsfromExcel`.
14. `ImportProductsfromExcel` delega a `Magento.ImportProductfromFileLine`.
15. El método abre el Excel como `DataSet`.
16. Recorre filas desde la 1.
17. Según la cantidad de columnas, elige modo.
18. Si son 2 columnas, actualiza stock de producto existente.
19. Si son 5 columnas, no hace nada.
20. Si es formato completo, valida headers.
21. Resuelve categoría a partir de `SUB CATEGORIA`.
22. Parsea dimensiones, peso, precios, IVA, marca y stock.
23. Si hay oferta positiva, pisa `Price`.
24. Arma `ProductApi`.
25. Hace `PUT` del producto.
26. Si la API dice “Producto inexistente”, hace `POST`.
27. Si `SincronizarImagenes == 1`, procesa imágenes.
28. Para cada imagen:
29. Descarga bytes desde URL remota.
30. Si hace falta, convierte WebP a JPEG.
31. Consulta si ya existe imagen en la API.
32. Si la imagen es distinta, hace `PUT`.
33. Si la imagen no existía, hace `POST`.
34. Si una fila falla, loguea y sigue.
35. Cuando termina el archivo, vuelve al servicio.
36. El servicio mueve el archivo a histórico.
37. Espera `timeZ`.
38. Repite para siempre.

---

## Limitaciones y rarezas concretas detectadas

### 1. Loop infinito adentro del evento del timer

El diseño es confuso: aparenta timer, pero opera como loop eterno.

### 2. `timeX` queda desdibujado

Existe en config, pero no gobierna la lógica principal visible del flujo.

### 3. Acepta `.xls`, pero usa reader de `.xlsx`

Hay una inconsistencia entre la validación de extensión y la librería usada.

### 4. El nombre del archivo no gobierna el provider real

Aunque el código parsea el nombre, el provider efectivo sale del loop de providers.

### 5. `CATEGORIA` se exige, pero no se usa para resolver categoría

La lógica real depende de `SUB CATEGORIA`.

### 6. Varias columnas se validan pero no impactan el payload

Especialmente:

- `TIPO`
- `AHORA`
- `FECHA DE INICIO`
- `FECHA DE FIN`

### 7. Error de estructura corta todo en la primera diferencia

No entrega un diagnóstico completo del archivo.

### 8. No hay resultado formal por SKU

Solo quedan logs, lo que complica auditoría operativa.

### 9. El archivo se mueve a histórico aunque haya errores parciales

No hay una carpeta intermedia de “procesado con error”.

### 10. La lógica de negocio y la integración HTTP están acopladas

Eso dificulta testeo, mantenimiento y reescritura.

---

## Conclusión final

El servicio legacy es un importador continuo, secuencial y bastante acoplado, que trabaja así:

- descubre providers desde base;
- busca archivos Excel por provider en filesystem;
- procesa cada archivo fila por fila;
- interpreta dos modos reales de importación:
  - stock-only con 2 columnas,
  - importación completa con layout largo;
- resuelve la clasificación del producto principalmente desde `SUB CATEGORIA`;
- intenta actualizar primero y crear después si el producto no existe;
- opcionalmente sincroniza imágenes comparando contenido;
- deja trazabilidad en logs, pero no genera un resultado de negocio estructurado por archivo ni por SKU.

Desde el punto de vista funcional, sí cumple con su objetivo básico de “levantar Excel e impactar productos e imágenes en una API REST”. Pero desde el punto de vista de diseño, observabilidad y robustez operativa, arrastra varias decisiones legacy:

- validaciones rígidas,
- mezcla de responsabilidades,
- falta de estados claros,
- falta de batch formal,
- falta de output de resultados amigable,
- y fuerte dependencia de hardcodeos y contratos implícitos.

Ese es, en términos prácticos, el comportamiento real del proceso legacy hoy.
