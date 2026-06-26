# Proceso funcional de carga de productos

## Objetivo de este documento

Este documento describe, en un nivel funcional y operativo, cómo funciona hoy el proceso de carga de productos.

La intención es dejar documentado el proceso real de punta a punta, pero sin entrar en detalles de código, librerías ni estructura interna de implementación.

En otras palabras, este documento busca responder:

- qué hace el proceso;
- qué entradas recibe;
- qué sistemas externos toca;
- qué decisiones toma;
- qué reglas de negocio aplica;
- qué resultados genera;
- qué comunicaciones emite;
- y cómo debe interpretarse cada caso desde negocio y operaciones.

Es un documento interno. Por eso incluye reglas funcionales, criterios operativos, integraciones externas, asuntos de correo, destinatarios, resultados y limitaciones relevantes del proceso.

---

## Qué problema resuelve este proceso

El proceso de carga de productos existe para que los sellers puedan informar productos y actualizaciones en forma masiva, mediante archivos Excel, sin depender de una carga manual producto por producto.

Desde el punto de vista del negocio, el proceso resuelve cuatro necesidades principales:

1. publicar productos nuevos;
2. actualizar información comercial o descriptiva de productos existentes;
3. actualizar stock en forma simple;
4. devolver una respuesta clara sobre qué se procesó bien, qué quedó parcial y qué requiere corrección.

También aporta orden operativo porque separa claramente:

- errores del archivo como archivo;
- errores de datos de productos;
- observaciones parciales, por ejemplo vinculadas a imágenes.

---

## Alcance del proceso

Hoy el proceso cubre estos casos:

- alta de productos nuevos;
- actualización general de ficha de productos existentes;
- actualización masiva de stock;
- baja comercial por stock `0`;
- carga de imágenes mediante links;
- resolución de categoría y subcategoría;
- tratamiento de ofertas;
- validación estructural del archivo;
- validación funcional de cada fila;
- devolución de resultado por correo.

Y hoy no debe pensarse como un proceso diseñado para:

- edición manual interactiva;
- corrección en vivo dentro de una pantalla;
- administración visual de catálogo;
- operación “registro por registro” con intervención humana entre pasos.

Es un proceso batch, orientado a archivos y a tratamiento masivo.

---

## Resultado funcional esperado

El resultado esperado del proceso es uno de estos dos:

- un archivo de resultados por fila, cuando la estructura del archivo fue válida;
- un archivo de errores estructurales, cuando el problema está en el formato general de la plantilla.

Además, si la configuración de notificaciones está activa y existe un destinatario válido, el proceso envía un correo con el adjunto correspondiente.

---

## Vista general del proceso

En una mirada resumida, el proceso hace esto:

1. identifica qué archivos hay para trabajar;
2. determina si pertenecen a providers válidos;
3. revisa la estructura del archivo;
4. si la estructura es válida, evalúa producto por producto;
5. aplica las reglas del tipo de carga correspondiente;
6. genera un archivo de salida entendible;
7. envía un correo con el adjunto correspondiente.

La idea central es que la carga se analiza en dos niveles distintos:

- nivel archivo;
- nivel fila.

Ese punto es muy importante.

## Nivel archivo

En este nivel, el proceso responde:

- ¿el archivo tiene la forma correcta?
- ¿corresponde a un formato soportado?
- ¿las columnas obligatorias existen?
- ¿hay duplicados de encabezado?

## Nivel fila

En este nivel, el proceso responde:

- ¿este producto tiene datos válidos?
- ¿el SKU es aceptable?
- ¿los números se pueden interpretar?
- ¿el stock es correcto?
- ¿la categoría puede resolverse?
- ¿las imágenes se pueden tratar?

La combinación de estos dos niveles explica por qué puede ocurrir que:

- un archivo completo sea rechazado por estructura;
- o que un archivo sea aceptado, pero algunas filas queden en error.

---

## Secuencia funcional completa de un archivo

Más allá del resumen general, la secuencia real de negocio de un archivo es esta:

1. el proceso detecta un archivo dentro de la carpeta de un provider válido;
2. asocia ese archivo a ese provider operativo;
3. mueve el archivo al estado `processing`;
4. abre la primera hoja del Excel;
5. identifica si el archivo corresponde al formato corto o al formato completo;
6. valida si la estructura general del archivo es aceptable;
7. si la estructura falla, genera `ErroresEstructura`, mueve el archivo a `processed` y termina ese caso;
8. si la estructura es válida, revisa fila por fila;
9. cada fila se interpreta como una unidad independiente de trabajo;
10. las filas vacías se omiten;
11. las filas con errores de datos quedan en `ERROR` sin llegar a impactar producto;
12. las filas válidas avanzan a lógica de stock o de carga completa, según el tipo de archivo;
13. al final se consolida un único archivo `Resultados`;
14. el archivo original se mueve a `processed`;
15. si las notificaciones están habilitadas y hay destinatarios, se envía el correo correspondiente.

## Qué significa “cada fila es independiente”

Esto es clave para entender el comportamiento del proceso.

Si una fila falla:

- no arrastra automáticamente a las demás;
- no invalida por sí sola un archivo estructuralmente correcto;
- y no impide que otras filas del mismo Excel sí queden `OK`.

Por eso un mismo archivo puede terminar con mezcla de:

- filas correctas;
- filas parciales;
- y filas con error.

---

## Tipos de archivo que acepta el proceso

Hoy el proceso acepta dos formatos funcionales.

## 1. Archivo completo

Este formato se usa cuando el seller necesita:

- dar de alta un producto nuevo;
- volver a subir una ficha completa;
- actualizar información general de una publicación existente.

En términos prácticos, este formato cubre cambios sobre:

- nombre;
- marca;
- descripción;
- medidas;
- peso;
- precio;
- IVA;
- categoría;
- subcategoría;
- stock;
- oferta;
- imágenes;
- y otros datos complementarios del layout.

### Estructura del archivo completo

El archivo completo tiene 19 columnas:

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

## 2. Archivo corto

Este formato se usa solo cuando el seller necesita cambiar stock.

No está pensado para:

- altas;
- cambios descriptivos;
- cambios comerciales amplios;
- correcciones de categoría;
- tratamiento de imágenes.

### Estructura del archivo corto

El archivo corto tiene 2 columnas:

- `SKU`
- `STOCK`

### Uso funcional del stock `0`

Si el seller informa stock `0`, el producto deja de figurar en la web.

Desde negocio, eso funciona como una baja comercial.

---

## Reglas generales del archivo

Antes de cualquier tratamiento funcional, el archivo tiene que cumplir ciertas reglas mínimas.

## Formato del archivo

El proceso espera un archivo Excel moderno.

Operativamente, eso implica trabajar con el formato Excel actual y no con formatos antiguos.

## Hoja utilizada

El proceso toma la primera hoja del archivo como hoja activa de trabajo.

## Encabezados

Los encabezados:

- deben existir;
- deben responder a uno de los formatos admitidos;
- no deben estar duplicados en los campos obligatorios.

## Tolerancia funcional sobre los encabezados

El proceso está pensado para reconocer los encabezados esperados aun cuando existan pequeñas diferencias de escritura que no cambien el sentido del campo.

Eso no significa que cualquier variante sea válida.

Significa que el proceso privilegia el reconocimiento funcional del campo por encima de una comparación totalmente rígida carácter por carácter.

## Qué sigue siendo obligatorio

Aunque exista cierta tolerancia en la lectura del encabezado:

- el campo correcto tiene que estar;
- el formato general tiene que coincidir;
- y no puede haber ambigüedad por duplicados.

## Filas vacías

Las filas completamente vacías no se consideran error funcional.

Simplemente se omiten.

---

## Lógica de aceptación o rechazo estructural

Lo primero que hace el proceso es revisar si el archivo “tiene forma” de archivo procesable.

Esto responde preguntas como:

- ¿tiene la cantidad de columnas esperada?
- ¿las columnas obligatorias están?
- ¿hay encabezados repetidos?

## Si la estructura es válida

El archivo avanza a la etapa de evaluación por fila.

## Si la estructura no es válida

El archivo no se procesa fila por fila.

En ese caso:

- se genera un archivo `ErroresEstructura`;
- el seller recibe una devolución estructural, no funcional por producto;
- la corrección debe hacerse en la plantilla completa.

Esto es importante porque evita perder tiempo revisando productos cuando el problema real está en el diseño del archivo.

## Qué cosas se consideran estructurales

En esta etapa, el proceso no analiza todavía si un producto está bien o mal cargado.

Analiza si el archivo, como archivo, puede ser interpretado.

Entre otras cosas, observa:

- si existe una fila de encabezados;
- si la cantidad de columnas coincide con alguno de los formatos soportados;
- si están presentes las columnas obligatorias del formato detectado;
- si hay columnas obligatorias duplicadas.

## Qué cosas no son estructurales

No se consideran errores estructurales:

- un SKU inválido en una fila;
- un stock mal escrito en una fila;
- una URL de imagen incorrecta en una fila;
- una subcategoría que luego termine en `Varios`.

Todos esos casos pertenecen a la etapa funcional por fila.

---

## Qué sistemas externos intervienen

Desde una mirada funcional interna, el proceso no vive aislado. Interactúa con varios sistemas externos.

Los principales son:

1. SQL Server, para obtener providers habilitados;
2. la API de productos, que expone distintos endpoints para productos, imágenes y subcategorías;
3. SendGrid, para las notificaciones por correo.

---

## Integración con SQL Server

El proceso obtiene desde SQL Server el listado de providers que efectivamente están habilitados para participar de la corrida.

## Qué busca en SQL Server

Busca providers válidos según tres criterios funcionales:

- habilitado;
- integrador correspondiente;
- catálogo correspondiente.

## Stored procedure utilizado

La lógica actual consulta el stored procedure:

`ProvidersGetListByEnabledAndIntegratorAndCatalogID`

## Datos que toma del provider

Del provider resultan relevantes, funcionalmente, estos campos:

- `ID`
- `Name`
- `Email`

## Para qué usa cada dato

- `ID`: determina qué carpeta del input corresponde a un provider válido;
- `Name`: enriquece el contexto del archivo y el asunto del correo;
- `Email`: se usa como posible destinatario de notificación.

---

## Integración con el filesystem

El proceso también interactúa con el filesystem de trabajo.

## Qué hace sobre el input

Lee el árbol de input y conserva solo:

- carpetas de providers válidos;
- archivos Excel dentro de esas carpetas.

## Regla importante de identificación del provider

El provider operativo sale de la carpeta válida del filesystem.

No se determina:

- por el contenido del Excel;
- por el nombre del archivo;
- ni por un dato embebido dentro de la planilla.

## Movimiento entre estados

Durante la vida del archivo, el proceso lo mueve entre tres estados operativos:

- `input`
- `processing`
- `processed`

Esto no es un detalle técnico menor: representa el estado funcional del trabajo.

### Sentido de cada estado

- `input`: archivo pendiente de ser tratado;
- `processing`: archivo en tratamiento;
- `processed`: archivo ya tratado, independientemente de si el resultado fue perfecto o con observaciones.

---

## Integración con la API de productos

El proceso usa una API de productos para consultar, crear o actualizar información de catálogo.

## Rutas funcionales principales

La API contempla, entre otras, estas rutas:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images/{index}`
- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images`
- `/Mp_ProductsAPI_CTC/subcategories/{providerID}/{subcategoryName}`

## Qué representa cada endpoint dentro del proceso

Cada endpoint cumple un rol funcional distinto:

- `/providers/{providerID}/products`: se usa para dar de alta un producto cuando no existe todavía para ese provider;
- `/providers/{providerID}/products/{sku}/`: se usa para consultar un producto puntual y también para actualizarlo cuando ya existe;
- `/providers/{providerID}/products/{sku}/images/{index}`: se usa para revisar o reemplazar una imagen en una posición determinada;
- `/providers/{providerID}/products/{sku}/images`: se usa para agregar una imagen nueva cuando esa posición todavía no existe;
- `/subcategories/{providerID}/{subcategoryName}`: se usa para intentar resolver una subcategoría informada en el Excel hacia una clasificación válida para el catálogo.

## Endpoint utilizado según la operación

Visto por operación de negocio, el uso es este:

## Consulta de producto existente

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/`

Se usa para:

- buscar el producto antes de modificar solo stock;
- y operar sobre un SKU puntual ya identificado.

## Actualización de producto existente

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/`

Se usa para:

- intentar actualizar un producto ya existente en archivo completo;
- y reenviar el producto con stock modificado en archivo corto.

## Alta de producto nuevo

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products`

Se usa para:

- crear un producto cuando el intento de actualización informa que ese SKU no existe todavía.

## Consulta de imagen existente

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images/{index}`

Se usa para:

- revisar si ya hay una imagen en esa posición;
- y comparar si el contenido actual ya coincide con el que se quiere subir.

## Reemplazo de imagen existente

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images/{index}`

Se usa para:

- actualizar una imagen cuando esa posición ya existe y el contenido cambió.

## Alta de imagen nueva

Se utiliza:

- `/Mp_ProductsAPI_CTC/providers/{providerID}/products/{sku}/images`

Se usa para:

- crear una imagen cuando no existe todavía una imagen válida en esa posición.

## Resolución de subcategoría

Se utiliza:

- `/Mp_ProductsAPI_CTC/subcategories/{providerID}/{subcategoryName}`

Se usa para:

- intentar transformar una subcategoría textual del Excel en una clasificación válida para catálogo.

## Qué hace el proceso contra la API de productos

Según el caso:

- consulta un producto existente;
- actualiza un producto;
- crea un producto nuevo;
- consulta imágenes existentes;
- actualiza imágenes;
- crea imágenes;
- consulta subcategorías por texto.

## Orden funcional de interacción con la API

La API de productos no se usa de forma aleatoria.

Según el tipo de archivo y el estado de cada fila, el orden lógico es este:

1. si es archivo corto, consulta el producto por SKU y luego lo actualiza con el nuevo stock;
2. si es archivo completo, primero resuelve la clasificación si hace falta;
3. luego intenta impactar el producto;
4. recién después, si corresponde, trata las imágenes;
5. en paralelo, si la clasificación no salió por mapeo conocido, consulta el endpoint de subcategorías.

Esto implica una dependencia fuerte entre etapas.

Por ejemplo:

- si no se puede impactar el producto, no se llega a imágenes;
- si la fila no pasa las validaciones previas, no llega ni siquiera a la API;
- si la estructura del archivo falla, ninguna fila llega a lógica de producto.

## Autenticación funcional

Las llamadas se realizan con autenticación y con formato JSON.

Desde el punto de vista de negocio, esto significa que:

- el proceso depende de una credencial válida;
- el proceso depende de que la API responda correctamente;
- el resultado final puede verse impactado si la API rechaza una operación.

---

## Lógica funcional del archivo corto

El archivo corto tiene una lógica muy específica: cambiar stock.

## Qué hace

Para cada fila válida:

1. identifica el producto por `SKU`;
2. consulta el producto existente en `/providers/{providerID}/products/{sku}/`;
3. toma el stock informado en el Excel;
4. reemplaza el stock anterior del producto encontrado;
5. vuelve a enviar el producto actualizado al mismo endpoint del SKU.

## Qué significa esto en términos de negocio

El archivo corto no se usa para “reconstruir” una ficha.

Se usa para tocar una sola variable operativa: la cantidad disponible para vender.

Eso implica que:

- el producto debe existir previamente;
- el SKU debe identificar exactamente al producto correcto;
- y la única modificación funcional buscada en este flujo es el stock.

## Qué no hace

No:

- crea productos nuevos;
- modifica atributos descriptivos;
- trata imágenes;
- resuelve categorías;
- corrige campos comerciales ajenos al stock.

## Qué pasa si el producto no existe

En este formato no existe una lógica de alta automática.

Si el producto no existe, la fila no puede cerrarse correctamente como una actualización de stock.

## Alcance real del cambio de stock

Cuando el stock se actualiza correctamente:

- si el nuevo stock es mayor a `0`, el producto queda comercialmente disponible;
- si el nuevo stock es `0`, el producto queda como baja comercial y deja de estar disponible para la web;
- el resto de la información del producto no debería cambiar por este flujo.

## Qué necesita realmente el proceso para cambiar stock

Para poder modificar stock con el archivo corto, el proceso necesita:

- que el `SKU` exista en la API para ese provider;
- que el stock pueda interpretarse como entero;
- que la API permita devolver el producto actual;
- y que luego acepte la actualización.

Esto es importante porque el archivo corto no “parcha” stock en el aire.

Primero recupera el producto actual y después reenvía la versión actualizada con el nuevo stock.

## Implicancia operativa del archivo corto

El archivo corto es más liviano para operar, pero también más limitado.

Sirve cuando el objetivo es estrictamente operativo:

- subir o bajar cantidad disponible;
- reflejar quiebres de stock;
- o sacar temporalmente un producto de la web dejando stock `0`.

No sirve como mecanismo para corregir ficha, precio, descripción, categoría o imágenes.

---

## Qué bloquea una fila antes de tocar la API

Antes de consultar o impactar productos, el proceso decide si la fila está en condiciones mínimas de negocio.

Una fila no llega a la API si presenta errores previos como:

- `SKU` faltante;
- `SKU` con caracteres no permitidos;
- campos obligatorios de texto vacíos en archivo completo;
- stock inválido;
- precio inválido;
- IVA inválido;
- medidas o peso inválidos;
- oferta con valor no numérico;
- URL de imagen mal formada dentro de la fila.

## Qué efecto tiene esto

Cuando ocurre uno de esos casos:

- la fila queda en `ERROR`;
- el detalle del problema se informa en `Resultados`;
- y no se intenta ni alta, ni actualización, ni imágenes para ese SKU.

## Límite operativo por fila

Cada fila tiene una ventana de ejecución acotada.

La idea de negocio detrás de esto es simple:

- una fila problemática no debe dejar bloqueado al archivo completo;
- una descarga lenta de imagen no debe frenar indefinidamente al resto;
- y el proceso tiene que poder cerrar la corrida con un resultado trazable.

Si una fila se corta por tiempo durante la etapa principal, queda en error.

Si se corta después de haber impactado el producto y mientras aún estaba tratando imágenes, puede quedar en `PARTIAL_OK`.

---

## Lógica funcional del archivo completo

El archivo completo concentra la mayor parte de la lógica de negocio del proceso.

## Qué hace

Para cada fila válida, el proceso:

1. toma la información general del producto;
2. valida campos obligatorios y numéricos;
3. resuelve la clasificación del producto;
4. prepara la información comercial y descriptiva;
5. intenta actualizar o crear el producto;
6. decide si corresponde tratar imágenes;
7. consolida el resultado por fila.

## Datos que realmente impacta este flujo

En este formato, la carga puede afectar la ficha comercial del producto en varios frentes al mismo tiempo:

- identificación del producto por `SKU`;
- nombre y marca;
- descripción y descripción corta;
- precio y oferta;
- stock;
- IVA;
- dimensiones y peso;
- clasificación comercial;
- imágenes.

Por eso este archivo es el camino correcto no solo para altas, sino también para cambios generales sobre productos ya existentes.

## Cómo interpreta funcionalmente esos datos

La carga completa no copia todo “tal cual viene” sin criterio.

Aplica además una interpretación funcional sobre algunos campos:

- el stock se toma como cantidad entera disponible;
- el precio se toma como valor comercial principal;
- la oferta, si viene informada, pasa a tener prioridad como precio promocional;
- el IVA se interpreta como tasa aplicable al producto;
- las dimensiones se toman como medidas físicas del producto;
- el peso informado en gramos se convierte internamente a la unidad operativa esperada por la API de productos;
- la categoría efectiva no sale solo del texto escrito, sino del proceso de resolución de clasificación.

Esto importa porque una fila puede estar “completa” visualmente en Excel y, aun así, requerir interpretación antes de convertirse en una actualización real de catálogo.

## Reglas funcionales clave del producto en archivo completo

Además de la validación básica, el proceso aplica estas decisiones funcionales:

- `SKU` identifica el producto de forma operativa para update o alta;
- `NOMBRE` alimenta tanto el nombre visible como la descripción corta;
- `MARCA` se conserva como parte de la ficha comercial;
- `DESCRIPCION` se toma como descripción principal del producto;
- `PRECIO` se conserva como base comercial de referencia;
- `OFERTA`, si viene informada y es mayor a `0`, pasa a ser el precio operativo del producto;
- el precio original sigue existiendo como referencia de lista;
- `IVA` entre `0` y `1` se interpreta como fracción y se convierte a porcentaje;
- `PESO` se recibe en gramos y se transforma a la unidad operativa consumida por la API;
- el producto se impacta como activo;
- la baja comercial no se maneja apagando el producto, sino principalmente por stock `0`.

## Qué pasa con precio, oferta y referencia comercial

La lógica de precios no se limita a guardar un único valor.

Funcionalmente:

- el precio informado en `PRECIO` actúa como valor base;
- si `OFERTA` no viene informada, el precio operativo queda igual al precio base;
- si `OFERTA` viene informada y es mayor a `0`, el proceso toma ese valor como precio operativo promocional;
- el precio original sigue sirviendo como referencia comercial.

Esto hace que el archivo completo sea el formato correcto no solo para altas, sino también para cambios de precio y de oferta.

## Qué pasa con campos del layout que hoy tienen menos peso operativo

Los campos `TIPO`, `AHORA`, `FECHA DE INICIO` y `FECHA DE FIN` forman parte del layout admitido.

Sin embargo, dentro de la lógica actual del proceso:

- no definen la aceptación de la fila por sí mismos;
- pueden venir vacíos;
- y no tienen el mismo impacto operativo que SKU, stock, precio, clasificación o imágenes.

## Qué significa “actualizar o crear”

Desde el punto de vista funcional:

- si el producto ya existe, se actualiza;
- si no existe, se intenta crear.

La lógica interna respeta la semántica heredada del proceso histórico:

- primero se intenta como actualización;
- si la API indica que el producto no existe, se pasa al camino de alta.

## Secuencia funcional contra la API de productos

Cuando una fila del archivo completo avanza correctamente, la secuencia esperada es esta:

1. el proceso arma la información comercial del producto con los datos del Excel;
2. intenta actualizar ese SKU en `/providers/{providerID}/products/{sku}/`;
3. si la API confirma que ese producto ya existe, la operación queda tratada como actualización;
4. si la API responde que el producto no existe, el proceso cambia de estrategia;
5. entonces intenta darlo de alta en `/providers/{providerID}/products`.

En otras palabras:

- primero intenta conservar la continuidad de un producto ya existente;
- y solo usa el endpoint de alta cuando la propia API indica que no hay nada previo para ese SKU.

---

## Lógica de clasificación: categoría y subcategoría

La clasificación del producto no depende únicamente de copiar el texto del Excel.

El proceso aplica una lógica específica para transformar la subcategoría enviada en una rama de catálogo efectiva.

## Regla principal

`CATEGORIA` y `SUB CATEGORIA` deben venir alineadas con el Marketplace de Club Patagonia, pero la resolución efectiva se apoya sobre todo en `SUB CATEGORIA`.

## Cadena de resolución actual

La lógica funcional sigue este orden:

1. intentar resolver por el mapeo de subcategorías precargado desde base de datos;
2. si no alcanza, consultar el endpoint de subcategorías de la API de productos;
3. si tampoco resuelve, usar la categoría de respaldo `Varios`.

## Qué significa esto en la práctica

Si la subcategoría coincide con un caso ya contemplado:

- el proceso la resuelve inmediatamente.

Si no coincide:

- intenta buscarla en el endpoint de subcategorías de la API de productos para ese provider.

Si tampoco obtiene una resolución útil:

- el producto cae en `Varios`.

## Qué campo pesa más en la resolución

Aunque el layout pide `CATEGORIA` y `SUB CATEGORIA`, la resolución efectiva se apoya especialmente en el valor informado en `SUB CATEGORIA`.

Ese dato es el que el proceso intenta traducir hacia una clasificación operativa válida dentro del catálogo.

Por eso, desde negocio:

- `CATEGORIA` debe seguir la nomenclatura esperada;
- `SUB CATEGORIA` debe estar lo más alineada posible con el Marketplace de Club Patagonia;
- y si la subcategoría no se puede resolver con seguridad, el producto puede terminar en `Varios`.

## Qué devuelve realmente esta etapa

La salida de esta lógica no es solo un texto de categoría.

Lo que el proceso necesita obtener es una rama de catálogo utilizable para impactar el producto en la API.

Por eso esta etapa define, en la práctica:

- dónde queda clasificado el producto;
- cómo se presenta comercialmente dentro del catálogo;
- y si pudo resolverse por mapeo conocido, por búsqueda en API o por fallback.

## Qué pasa si `CATEGORIA` y `SUB CATEGORIA` no ayudan de la misma manera

Si ambos campos vienen informados, pero la subcategoría no logra una resolución útil:

- el proceso no se detiene automáticamente por contradicción textual;
- intenta igualmente llegar a una clasificación operativa;
- y si no encuentra una mejor alternativa, usa `Varios`.

Eso significa que una fila puede completar su impacto de producto y, al mismo tiempo, quedar publicada en una clasificación genérica.

## Valor funcional de esta lógica

Esto evita que cada pequeña diferencia de redacción en la subcategoría rompa toda la carga, pero también implica que una carga mal clasificada puede terminar publicada en una categoría comodín.

---

## Integración con el endpoint de subcategorías de la API de productos

Cuando no alcanza el mapeo precargado, el proceso consulta el endpoint de subcategorías dentro de la misma API de productos.

## Qué busca

Busca coincidencias por:

- provider;
- texto de subcategoría informado.

## Qué decisión toma con la respuesta

Si obtiene una lista de resultados:

- toma la primera coincidencia útil.

Si no obtiene coincidencia:

- el proceso no se detiene por eso;
- continúa con la categoría de respaldo `Varios`.

Esto es importante porque muestra que la resolución de categoría es robusta, pero no perfecta. Tiene mecanismos de continuidad para evitar bloquear toda una carga por una clasificación no encontrada.

## Consecuencia operativa de esta lógica

La clasificación no funciona como un simple copiado textual.

Funciona como un proceso de resolución.

Eso significa que dos productos con textos parecidos pueden terminar:

- bien clasificados, si la subcategoría encuentra correspondencia;
- o en `Varios`, si esa correspondencia no aparece.

Por eso una carga puede quedar técnicamente procesada, pero comercialmente requerir revisión de categoría.

---

## Lógica de imágenes

Las imágenes se informan a través del campo `URL IMAGENES`.

## Cómo se interpreta el campo `URL IMAGENES`

El proceso no espera una columna por imagen.

Espera una sola celda que puede contener:

- ninguna imagen;
- una sola imagen;
- o varias imágenes separadas entre sí.

## Regla de separación de múltiples imágenes

Cuando una fila informa varias imágenes, el proceso las separa usando el carácter `&`.

Después:

- limpia espacios sobrantes;
- descarta segmentos vacíos;
- y valida que cada link tenga forma válida de URL.

## Restricción sobre el tipo de link

Para que una URL de imagen sea aceptable, tiene que ser una dirección web válida y usable.

En la práctica:

- debe tener formato de URL real;
- debe apuntar a un host concreto;
- y debe usar `http` o `https`.

## Qué pasa si una URL está mal formada

Si una URL de imagen viene mal formada, eso ya genera un error funcional de fila.

Es decir:

- no se trata como un detalle menor;
- y puede impedir que la fila llegue siquiera a la etapa de sincronización de imágenes.

## Cuándo intervienen

Solo intervienen si:

- la sincronización de imágenes está habilitada globalmente;
- la fila llegó correctamente a la etapa de imágenes;
- y el campo `URL IMAGENES` contiene links válidos.

## Qué hace el proceso con cada imagen

Para cada imagen:

1. descarga el contenido remoto;
2. si hace falta, adapta el formato para que sea aceptable;
3. consulta si ya existe una imagen en ese índice usando `/providers/{providerID}/products/{sku}/images/{index}`;
4. si existe y es idéntica, la omite;
5. si existe pero cambió, la reemplaza en ese mismo índice;
6. si el índice no existe, la crea como imagen nueva usando `/providers/{providerID}/products/{sku}/images`.

## Tratamiento funcional del formato de imagen

Si la imagen descargada ya viene en un formato estándar utilizable, el proceso trabaja con esa misma imagen.

Si no puede tratarla directamente pero detecta un caso compatible de conversión, la adapta antes de enviarla a la API.

Esto busca evitar que una imagen útil falle solo por una diferencia de formato de origen.

## Regla de orden

El orden de las imágenes sigue el orden en que los links fueron informados.

Esto significa que la primera URL informada conserva la primera posición, la segunda conserva la segunda posición, y así sucesivamente según el orden recibido.

Desde negocio, este punto es importante porque el orden de los links impacta en el orden visual esperado del producto.

## Qué condiciones deben darse para que una fila llegue a imágenes

La etapa de imágenes nunca es la primera.

Una fila solo llega a esta instancia si antes ocurrió todo esto:

1. la estructura del archivo fue válida;
2. la fila pasó sus validaciones propias;
3. la clasificación pudo resolverse;
4. el producto pudo ser impactado en la API;
5. la sincronización global de imágenes está habilitada;
6. la fila trae al menos una URL válida.

Si alguno de esos puntos no se cumple:

- la fila no entra en sincronización de imágenes;
- o bien queda en error antes;
- o bien termina `OK` con imágenes `NO_APLICA`.

## Qué pasa si algunas imágenes fallan

Si el producto avanzó, pero una o más imágenes no pudieron resolverse completamente:

- el resultado puede quedar en `PARTIAL_OK`.

Esto significa que el producto no necesariamente está perdido, pero sí requiere revisión puntual.

---

## Integración con los endpoints de imágenes de la API de productos

La lógica de imágenes se apoya en rutas específicas de la API de productos:

- consulta de imagen por índice;
- actualización de imagen por índice;
- creación de imagen nueva.

## Cadena funcional de decisión

La lógica actual es:

1. consultar imagen existente;
2. si es igual, no volver a subirla;
3. si cambió, intentar actualizarla;
4. si ese índice no existe, crearla.

## Qué protege esta decisión

Esta secuencia busca mantener estabilidad operativa:

- no vuelve a subir imágenes sin necesidad;
- no borra imágenes por defecto;
- intenta respetar la posición de cada imagen;
- y separa claramente el caso “imagen igual”, “imagen cambiada” e “imagen inexistente”.

## Cuándo una fila queda `PARTIAL_OK` por imágenes

El caso parcial aparece especialmente cuando el producto principal ya quedó impactado, pero la etapa visual no cerró por completo.

Eso puede pasar, por ejemplo, si:

- una imagen no pudo descargarse;
- una imagen respondió con error al intentar sincronizarla;
- la fila llegó al timeout durante la etapa de imágenes;
- o se sincronizó solo una parte de las imágenes antes de un corte.

En ese escenario, el proceso conserva una idea muy importante:

- no “deshace” el producto que ya logró impactar;
- pero tampoco lo presenta como un éxito pleno.

Por eso informa `PARTIAL_OK`.

## Beneficio funcional

Esto evita subidas innecesarias, reduce ruido operativo y mantiene una semántica cercana al comportamiento histórico del proceso.

---

## Reglas funcionales del SKU

El `SKU` es el identificador operativo del producto dentro de la carga.

## Reglas clave

El `SKU`:

- no puede estar vacío cuando es obligatorio;
- no puede incluir espacios;
- no puede incluir puntos;
- no puede incluir barras;
- no puede incluir símbolos especiales no permitidos.

Esto es relevante porque un SKU mal formado no solo afecta la interpretación del Excel, sino también la posibilidad de interactuar correctamente con la API de productos.

---

## Reglas numéricas

El proceso recibe varios campos numéricos:

- alto;
- ancho;
- largo;
- peso;
- precio;
- IVA;
- stock;
- oferta.

## Qué flexibilidad existe

En los campos decimales se toleran variantes habituales, como:

- coma decimal;
- punto decimal.

## Qué no se tolera en stock

El stock debe ser entero.

No debe venir:

- con decimales;
- con texto;
- ni con aclaraciones mezcladas.

## Unidades esperadas

- `ALTO`: centímetros
- `ANCHO`: centímetros
- `LARGO`: centímetros
- `PESO`: gramos

Estas unidades son parte de la interpretación funcional del archivo.

---

## Reglas de oferta

El campo `OFERTA` es opcional.

## Si viene informado

Debe ser numérico.

## Si se quiere quitar una oferta

La regla operativa actual es:

- volver a subir el producto con `OFERTA` vacío.

Esto es importante porque el proceso necesita una instrucción explícita para retirar ese dato de la carga siguiente.

---

## Reglas de campos opcionales complementarios

Los campos `TIPO`, `AHORA`, `FECHA DE INICIO` y `FECHA DE FIN` forman parte del layout completo.

Desde la lógica funcional actual:

- no son campos críticos para la aceptación de la fila;
- pueden dejarse vacíos;
- si el negocio los usa, pueden informarse.

No tienen el mismo peso funcional que:

- SKU;
- stock;
- precio;
- categoría;
- subcategoría;
- imágenes.

---

## Qué resultados puede tener una fila

Cada fila puede terminar, funcionalmente, en uno de estos estados principales:

- `OK`
- `PARTIAL_OK`
- `ERROR`

Además, las filas totalmente vacías se omiten.

## `OK`

Significa que la fila terminó correctamente.

## `PARTIAL_OK`

Significa que la fila avanzó, pero alguna parte no quedó completamente cerrada.

El caso típico es imágenes.

## `ERROR`

Significa que esa fila no pudo resolverse correctamente y requiere corrección.

## Casos típicos de cada estado

## Casos típicos de `OK`

- stock actualizado correctamente en archivo corto;
- producto actualizado correctamente en archivo completo sin imágenes;
- producto creado o actualizado correctamente con imágenes tratadas sin fallas.

## Casos típicos de `PARTIAL_OK`

- producto impactado, pero la categoría terminó en `Varios` por no poder resolverse la subcategoría;
- producto impactado, pero una o más imágenes fallaron;
- producto impactado, pero la fila se quedó sin tiempo durante imágenes;
- producto impactado con parte visual incompleta.

## Casos típicos de `ERROR`

- la fila no pasó validaciones previas;
- la API de productos rechazó la operación principal;
- el producto no existía en un archivo corto de solo stock.

---

## Resultados visibles para negocio

Cuando el archivo pasa la etapa estructural, el resultado funcional visible se expresa en un archivo `Resultados`.

Ese archivo expone, por fila:

- el número de fila original;
- el SKU;
- el estado final;
- el resultado del producto;
- el resultado de imágenes;
- un mensaje resumido;
- un detalle complementario.

Esto cumple un rol clave: transformar una carga masiva en una devolución legible para personas.

## Qué filas aparecen y cuáles no

En `Resultados` aparecen las filas que realmente contenían un SKU o un intento de carga relevante.

Las filas completamente vacías:

- no se tratan como error;
- y tampoco se escriben en el archivo final de resultados.

Esto ayuda a que el adjunto quede enfocado en lo que verdaderamente requiere lectura operativa.

## Cómo leer `resultado del producto` y `resultado de imágenes`

Estas dos columnas deben leerse por separado.

No describen la misma parte del proceso.

## `resultado del producto`

Indica qué pasó con la ficha principal del SKU.

Resume si el proceso pudo:

- validar la fila;
- identificar el producto correcto;
- actualizarlo;
- o darlo de alta.

## `resultado de imágenes`

Indica qué pasó con la parte visual del producto.

Resume si el proceso pudo:

- no tratar imágenes porque no correspondía;
- mantener imágenes ya existentes sin cambios;
- actualizar imágenes;
- crear imágenes nuevas;
- o detectar fallas puntuales en esa etapa.

## Por qué esta separación es importante

Un producto puede quedar correctamente tratado a nivel comercial y, aun así, tener observaciones en imágenes.

Ese escenario es uno de los motivos por los que puede aparecer un `PARTIAL_OK`.

---

## Diferencia entre `Resultados` y `ErroresEstructura`

## `Resultados`

Significa que:

- el archivo fue aceptado estructuralmente;
- se pudieron revisar las filas;
- el detalle se da fila por fila.

No implica que todo haya salido perfecto.

Implica que el archivo fue lo suficientemente correcto como para entrar en la etapa funcional.

## `ErroresEstructura`

Significa que:

- el problema está en el archivo como archivo;
- no tiene sentido revisar producto por producto;
- primero hay que corregir la plantilla o la estructura.

---

## Cómo se define el estado final del archivo

El archivo completo también termina con un estado general, no solo cada fila.

La lógica es esta:

- si falló la estructura, el archivo queda rechazado por estructura;
- si la estructura fue válida y todas las filas procesadas quedaron `OK`, el archivo queda como procesado correctamente;
- si la estructura fue válida, pero existe al menos una fila `ERROR` o `PARTIAL_OK`, el archivo queda como procesado con errores u observaciones.

## Qué significa “procesado con errores”

No significa necesariamente que todo el archivo salió mal.

Significa que:

- el archivo sí se pudo trabajar;
- hubo impacto real en una o más filas;
- pero no todas las filas terminaron perfectas.

Este es un punto muy importante para operación, porque evita leer como “rechazo total” algo que en realidad fue un procesamiento parcial o mixto.

---

## Integración con SendGrid

La capa de mailing utiliza SendGrid como proveedor de envío de correo.

Desde el punto de vista funcional interno, esto significa que:

- el proceso no manda correos de forma local o manual;
- delega el envío real a SendGrid;
- adjunta el archivo de resultado correspondiente y también el archivo original ya procesado;
- y considera fallido el envío si SendGrid responde con error.

## Tipo de envío

El proceso arma un correo de texto simple con:

- un remitente configurado;
- una lista de destinatarios;
- un asunto dinámico;
- un cuerpo corto;
- hasta dos adjuntos.

---

## Cuándo se envía correo

El proceso intenta notificar al finalizar el tratamiento de un archivo en estos casos:

1. cuando el archivo terminó con resultado funcional;
2. cuando el archivo terminó rechazado por estructura.

## Cuándo no se envía

Puede no enviarse si:

- las notificaciones están deshabilitadas;
- no se pudieron resolver destinatarios;
- no existe adjunto disponible para enviar.

En esos casos:

- el proceso registra la situación;
- pero no considera que eso invalide el trabajo ya hecho sobre el archivo.

---

## Destinatarios del correo

La resolución de destinatarios sigue una lógica simple y estable.

## Regla actual

Se arma la lista final sumando:

1. los destinatarios fijos definidos en configuración;
2. el email del provider, si existe.

Después:

- se eliminan vacíos;
- se eliminan duplicados;
- se ordena el resultado.

## Consecuencia operativa

Esto permite que siempre exista una base de destinatarios internos o de soporte, y además se incorpore el contacto operativo del provider cuando está disponible.

---

## Remitente del correo

El remitente sale de la configuración de notificaciones.

Desde negocio, esto significa que la salida del proceso tiene una identidad formal y no depende de una cuenta manual de quien esté ejecutando la carga.

---

## Asuntos de correo

El asunto del correo depende del estado final del archivo.

## Si el archivo fue rechazado por estructura

El asunto es:

`Archivo rechazado - {providerName} - {filename}`

## Si el archivo fue procesado con errores u observaciones

El asunto es:

`Archivo procesado con errores - {providerName} - {filename}`

## Si el archivo fue procesado correctamente

El asunto es:

`Archivo procesado - {providerName} - {filename}`

## Importancia funcional del asunto

El asunto ya da una primera señal clara del resultado sin necesidad de abrir el adjunto.

Eso mejora mucho la lectura operativa del correo.

---

## Cuerpo de los correos

El cuerpo del correo es deliberadamente corto.

No intenta reemplazar al adjunto.

Su función es solo contextualizar el envío.

## Cuerpo en rechazo estructural

Mensaje base:

`El archivo adjunto no pudo procesarse por estructura invalida.`

## Cuerpo en archivo procesado con observaciones

Mensaje base:

`Se proceso el archivo adjunto con observaciones.`

## Cuerpo en archivo procesado

Mensaje base:

`Se proceso el archivo adjunto.`

## Qué se espera del usuario

La lectura real del resultado no está en el cuerpo del correo, sino en el archivo adjunto.

---

## Qué archivo se adjunta en cada correo

## Si el archivo terminó con resultado funcional

Se adjunta:

- el archivo `Resultados`;
- y el archivo original ya movido a `processed`.

## Si el archivo terminó con error estructural

Se adjunta:

- el archivo `ErroresEstructura`;
- y el archivo original ya movido a `processed`.

## Qué implica esto

El adjunto no es decorativo.

Es el soporte principal de la devolución funcional.

---

## Qué pasa si SendGrid falla

Si SendGrid falla:

- el error se registra;
- pero el resultado funcional del archivo no se revierte.

Esto es importante porque separa:

- el éxito o fracaso de la carga;
- del éxito o fracaso de la notificación.

Desde negocio, una cosa no invalida automáticamente la otra.

---

## Qué hacer según el resultado recibido

## Si llega `Resultados`

Conviene:

1. revisar cuántas filas quedaron `OK`;
2. identificar si hubo `PARTIAL_OK`;
3. revisar las filas `ERROR`;
4. corregir solo lo necesario;
5. volver a cargar lo que corresponda ajustar.

## Si llega `ErroresEstructura`

Conviene:

1. revisar la estructura del archivo;
2. corregir columnas, nombres o formato;
3. guardar nuevamente la plantilla;
4. volver a subir el archivo completo.

---

## Casos de uso más frecuentes

## Alta de producto nuevo

Corresponde usar:

- archivo completo.

## Actualización general de producto existente

Corresponde usar:

- archivo completo.

## Cambio solo de stock

Corresponde usar:

- archivo corto.

## Baja comercial

Corresponde:

- informar stock `0`.

## Corrección de oferta

Si se quiere quitar una oferta:

- volver a subir el producto con `OFERTA` vacío.

## Corrección de clasificación

Si un producto quedó mal clasificado:

- revisar categoría y subcategoría contra Club Patagonia;
- volver a subir la ficha corregida.

---

## Consideraciones operativas importantes

Hay varios puntos que conviene tener siempre presentes.

### 1. No todo error significa lo mismo

No es lo mismo:

- un archivo con error estructural;
- una fila con error;
- una fila parcial por imágenes.

### 2. El detalle real está en el adjunto

El correo avisa, pero el diagnóstico funcional está en el archivo adjunto.

### 3. El archivo corto simplifica mucho las actualizaciones de stock

Cuando la necesidad es solo disponibilidad, conviene usar el archivo corto.

### 4. El archivo completo debe reservarse para altas o cambios generales

Eso evita trabajo innecesario y reduce superficie de error.

### 5. La clasificación incorrecta no necesariamente bloquea toda la carga

Pero sí puede derivar en `Varios`, con impacto comercial sobre la publicación.

---

## Revisión operativa antes de cada carga

Antes de subir un archivo, conviene revisar:

- que la plantilla no haya sido alterada;
- que el archivo esté en formato Excel;
- que los campos obligatorios estén completos;
- que el SKU esté bien escrito;
- que el stock sea entero;
- que categoría y subcategoría coincidan con Club Patagonia;
- que las imágenes tengan links válidos si se informan;
- que las medidas y el peso estén en las unidades esperadas;
- que la intención de negocio coincida con el archivo elegido.

---

## Valor que aporta este proceso

Más allá de la ejecución operativa, este proceso aporta valor porque:

- permite cargas masivas;
- baja dependencia de carga manual;
- separa claramente errores de estructura y de producto;
- da una devolución entendible;
- conserva trazabilidad funcional;
- y habilita corrección dirigida en lugar de retrabajo ciego.

---

## Resumen ejecutivo final

Si hubiera que sintetizar el proceso en pocas ideas clave, serían estas:

1. El seller envía un archivo Excel.
2. El proceso primero revisa si la estructura del archivo es válida.
3. Si la estructura está bien, analiza producto por producto.
4. El archivo corto sirve solo para stock.
5. El archivo completo sirve para altas y actualizaciones generales.
6. La clasificación depende de categoría y, sobre todo, subcategoría.
7. Si la categoría no puede resolverse, el producto cae en `Varios`.
8. Las imágenes se tratan por orden de links informados.
9. El stock `0` funciona como baja comercial.
10. El resultado final puede expresarse como `Resultados` o `ErroresEstructura`.
11. La notificación se envía por SendGrid.
12. El correo cambia asunto, cuerpo y adjunto según el estado final del archivo.

---

## Conclusión

El proceso funcional de carga de productos hoy ya resuelve el ciclo completo de:

- recepción del archivo;
- validación estructural;
- evaluación por fila;
- tratamiento de stock;
- tratamiento de ficha completa;
- resolución de clasificación;
- tratamiento de imágenes;
- generación de resultado;
- y devolución por correo.

Desde una mirada interna de negocio y operaciones, eso significa que existe una lógica consistente, trazable y estable para convertir una planilla enviada por un seller en una devolución clara y accionable.
