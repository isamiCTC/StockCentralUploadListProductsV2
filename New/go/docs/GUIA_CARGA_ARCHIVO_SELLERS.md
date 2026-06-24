# Guía de carga de productos para sellers

## Introducción

Esta guía fue preparada para acompañarte en la carga de productos de Club Patagonia.

Su objetivo es ayudarte a completar correctamente el archivo Excel que se utiliza para publicar productos nuevos o actualizar información ya existente.

No hace falta tener conocimientos técnicos para usarla. Está pensada para que puedas seguirla paso a paso, con ejemplos simples y con explicaciones claras.

La recomendación es usar este documento como referencia cada vez que prepares una carga.

---

## ¿Para qué sirve este archivo?

El archivo de carga sirve para subir información de productos al portal de sellers de Club Patagonia.

Vas a usar uno de estos dos formatos:

- archivo completo: para dar de alta productos nuevos o actualizar datos generales de un producto;
- archivo corto: para cambiar solo stock.

---

## Antes de empezar

Antes de completar el archivo, te recomendamos leer estas reglas generales. Son simples, pero muy importantes para que la carga pueda tomarse correctamente.

- El archivo tiene que estar en formato Excel (técnicamente, `.xlsx`).
- No uses formatos viejos de Excel, como `.xls`.
- Trabajá siempre sobre la primera hoja del archivo.
- No cambies los nombres de las columnas.
- No borres columnas.
- No agregues columnas nuevas.
- La primera fila tiene que ser la fila de títulos.
- Cada fila tiene que corresponder a un único producto.

Si el archivo no respeta esta estructura, puede ocurrir que:

- no se tome correctamente;
- se rechace por estructura;
- algunos productos no puedan procesarse.

Por eso, antes de subirlo, siempre conviene revisar que la plantilla siga intacta.

---

## Elegir el formato correcto

Uno de los errores más comunes es usar el archivo equivocado para la tarea que querés hacer. Para evitar eso, esta es la regla práctica:

## Cuándo usar el archivo completo

Usá el archivo completo cuando necesites:

- dar de alta un producto nuevo;
- actualizar la ficha general de un producto ya existente, por ejemplo precio, descripción, marca, medidas, categoría, subcategoría o stock.

Este archivo tiene 19 columnas:

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

## Cuándo usar el archivo corto

Usá el archivo corto cuando necesites:

- cambiar únicamente el stock de un producto.

Importante:

- si querés dar de baja un producto, podés dejar su stock en `0`;
- cuando un producto queda con stock `0`, deja de figurar en la web.

Este archivo tiene solo 2 columnas:

- `SKU`
- `STOCK`

---

## Cómo completar el archivo corto

El archivo corto está pensado para una tarea puntual y simple: actualizar stock de productos que ya existen.

Es el formato más rápido cuando no necesitás modificar el resto de la ficha.

## Campos del archivo corto

En este formato, ambos campos son obligatorios:

- `SKU`
- `STOCK`

## Explicación de cada campo

### `SKU`

Es el código del producto.

Tiene que estar siempre completo.

Para evitar errores:

- usá solo letras, números, guion `-` y guion bajo `_`.

No uses:

- espacios, puntos, barras ni símbolos especiales.

Ejemplos correctos:

- `TV123`
- `TV-123`
- `TV_123`

Ejemplos incorrectos:

- `TV 123`
- `TV.123`
- `TV/123`
- `TV#123`

### `STOCK`

Es la cantidad disponible del producto.

También es obligatorio.

Tiene que estar cargado como número entero.

Eso significa:

- sí podés poner `0`, `5`, `18`;
- no uses texto ni decimales.

Ejemplos correctos:

- `0`
- `5`
- `18`

Ejemplos incorrectos:

- `5,5`
- `5.5`
- `diez`
- `10 unidades`

## Ejemplo de archivo corto

| SKU | STOCK |
|---|---:|
| TV123 | 15 |
| HELADERA_20 | 4 |
| LICUADORA-ROJA | 0 |

## Recomendación para este formato

Si solo necesitás cambiar cantidades, usá este archivo. Es más simple, más rápido y te evita completar información que no hace falta tocar.

---

## Cómo completar el archivo completo

El archivo completo se usa para altas de productos.

Es el formato ideal cuando querés informar toda la ficha del producto desde el inicio.

## Reglas generales del archivo completo

Te sugerimos trabajar con estas pautas:

- completá una fila por producto;
- no dejes vacíos los campos obligatorios;
- en los números podés usar coma o punto decimal;
- revisá bien el `SKU` antes de subir.

## Campos obligatorios y campos opcionales

### Campos obligatorios

Estos campos tienen que estar completos:

- `SKU`
- `NOMBRE`
- `MARCA`
- `DESCRIPCION`
- `ALTO`
- `ANCHO`
- `LARGO`
- `PESO`
- `PRECIO`
- `IVA`
- `CATEGORIA`
- `SUB CATEGORIA`
- `STOCK`

### Campos opcionales

Estos campos pueden quedar vacíos:

- `URL IMAGENES`
- `TIPO`
- `AHORA`
- `OFERTA`
- `FECHA DE INICIO`
- `FECHA DE FIN`

Importante:

- los campos opcionales pueden dejarse vacíos;
- si decidís completarlos, hacelo solo cuando corresponda.

---

## Explicación simple de cada columna del archivo completo

### `SKU`

Es el código del producto.

Es obligatorio.

Usá solo:

- letras, números, guion `-` y guion bajo `_`.

Te recomendamos revisarlo con atención antes de subir el archivo, porque es uno de los datos más importantes de la carga.

Importante:

- el `SKU` no puede incluir puntos.

Ejemplo incorrecto:

- `TV.123`

### `NOMBRE`

Es el nombre del producto.

Es obligatorio.

Conviene escribirlo de forma clara, comercial y fácil de identificar.

Ejemplo:

- `Smart TV 50 pulgadas`

### `MARCA`

Es la marca del producto.

Es obligatoria.

Ejemplo:

- `Samsung`

### `DESCRIPCION`

Es la descripción del producto.

Es obligatoria.

Te recomendamos usar una descripción clara, breve y fácil de entender.

Ejemplo:

- `Smart TV 50 pulgadas 4K con control remoto`

### `ALTO`

Es obligatorio.

Tiene que ser un número.

Completalo en centímetros.

Ejemplos:

- `10`
- `10,5`
- `10.5`

### `ANCHO`

Es obligatorio.

Tiene que ser un número.

Completalo en centímetros.

### `LARGO`

Es obligatorio.

Tiene que ser un número.

Completalo en centímetros.

### `PESO`

Es obligatorio.

Tiene que ser un número.

Completalo en gramos.

### `URL IMAGENES`

Acá van los links de las imágenes del producto.

Este campo es opcional.

Si lo completás:

- cada link tiene que empezar con `http://` o `https://`;
- si cargás varias imágenes, separalas con `&`;
- el orden de las imágenes va a ser el mismo orden en el que escribas los links;
- no pongas texto que no sea un link.

Ejemplo de una imagen:

`https://miweb.com/imagen1.jpg`

Ejemplo de varias imágenes:

`https://miweb.com/imagen1.jpg & https://miweb.com/imagen2.jpg`

Importante:

- si completás este campo, los links informados reemplazan los links anteriores del producto.

### `PRECIO`

Es obligatorio.

Tiene que ser un número.

Ejemplos:

- `15000`
- `15000,50`
- `15000.50`

### `IVA`

Es obligatorio.

Tiene que ser un número.

Ejemplos:

- `21`
- `10,5`
- `0,21`

### `TIPO`

Este campo es opcional.

Si tu operación usa este dato, completalo.

Si no lo usás, podés dejarlo vacío.

### `AHORA`

Este campo es opcional.

Si tu operación usa este dato, completalo.

Si no lo usás, podés dejarlo vacío.

### `CATEGORIA`

Es obligatoria.

Tiene que coincidir exactamente con una categoría del Marketplace de Club Patagonia:

https://www.clubpatagonia.com.ar/

Lo más recomendable es copiarla tal como figura en el sitio.

No uses una categoría inventada ni una parecida.

### `SUB CATEGORIA`

Es obligatoria.

Tiene que coincidir exactamente con una subcategoría del Marketplace de Club Patagonia:

https://www.clubpatagonia.com.ar/

Importante:

- `CATEGORIA` y `SUB CATEGORIA` tienen que coincidir exactamente con las del Marketplace;
- si no coinciden, al producto se le va a asignar por defecto la categoría `Varios`.

### `STOCK`

Es obligatorio.

Tiene que ser un número entero.

### `OFERTA`

Este campo es opcional.

Si no hay oferta, podés dejarlo vacío.

Si lo completás:

- tiene que ser un número.

Si querés quitar una oferta vigente, volvé a subir el producto con el campo `OFERTA` vacío.

### `FECHA DE INICIO`

Este campo es opcional.

Si la usás, completala.

Si no la usás, podés dejarla vacía.

### `FECHA DE FIN`

Este campo es opcional.

Si la usás, completala.

Si no la usás, podés dejarla vacía.

---

## Ejemplo de una fila completa

### Ejemplo - Parte 1

| SKU | NOMBRE | MARCA | DESCRIPCION | ALTO | ANCHO | LARGO | PESO | URL IMAGENES | PRECIO |
|---|---|---|---|---:|---:|---:|---:|---|---:|
| TV50-4K | Smart TV 50 pulgadas | Samsung | Smart TV 50 pulgadas 4K | 72,5 | 112 | 8,1 | 12800 | https://miweb.com/tv1.jpg & https://miweb.com/tv2.jpg | 550000 |

### Ejemplo - Parte 2

| IVA | TIPO | AHORA | CATEGORIA | SUB CATEGORIA | STOCK | OFERTA | FECHA DE INICIO | FECHA DE FIN |
|---:|---|---|---|---|---:|---:|---|---|
| 21 | TV | 12 | Tecnología | Televisores | 8 | 499999 | 01/07/2026 | 15/07/2026 |

## Qué tener en cuenta con este ejemplo

Este ejemplo es solo orientativo.

Lo importante no es copiar exactamente esos valores, sino respetar:

- el nombre de las columnas;
- el tipo de dato que va en cada una;
- las unidades de medida;
- y el formato general del archivo.

---

## Errores comunes y cómo evitarlos

### 1. Falta una columna

Qué puede pasar:

- el archivo no se va a poder tomar correctamente.

Cómo evitarlo:

- no borres columnas;
- no cambies los títulos.

### 2. Hay columnas repetidas

Qué puede pasar:

- el archivo no se va a poder tomar correctamente.

Cómo evitarlo:

- revisá que un mismo título no aparezca dos veces.

### 3. El SKU tiene caracteres incorrectos

Qué puede pasar:

- ese producto puede dar error.

Cómo evitarlo:

- usá solo letras, números, `-` y `_`.

### 4. Un número está escrito como texto

Ejemplos:

- `diez`
- `sin stock`
- `aprox 15`

Qué puede pasar:

- ese producto puede dar error.

Cómo evitarlo:

- escribí solo números.

### 5. El stock tiene decimales

Ejemplo:

- `5,5`

Qué puede pasar:

- ese producto puede dar error.

Cómo evitarlo:

- en `STOCK` usá solo enteros.

### 6. La categoría o subcategoría no coincide

Qué puede pasar:

- si no coincide exactamente con las del Marketplace de Club Patagonia, el producto puede quedar en `Varios`.

Cómo evitarlo:

- copiá la categoría y la subcategoría exactamente como figuran en el sitio.

### 7. La URL de imagen está mal escrita

Ejemplos:

- `foto1`
- `www.miweb.com/imagen.jpg`

Qué puede pasar:

- ese producto puede dar error.

Cómo evitarlo:

- usá links completos que empiecen con `http://` o `https://`.

### 8. Se usó el archivo equivocado

Qué puede pasar:

- querés hacer una alta o una actualización general usando un formato que solo sirve para stock.

Cómo evitarlo:

- usá el archivo completo para altas;
- usá el archivo corto solo para cambios de stock.

---

## Qué pasa después de subir el archivo

Una vez que subas el archivo al backoffice de sellers de Club Patagonia, si previamente nos proporcionaste un correo electrónico, vas a recibir un mail cuando el archivo haya sido tomado.

Ese correo puede llegarte tanto si todo salió bien como si hubo observaciones o errores.

Por eso, después de cada carga, te recomendamos revisar el mail y abrir el archivo adjunto.

En ese mail podés recibir uno de estos dos casos:

## Caso 1: recibís un archivo `Resultados`

Este es el caso más habitual.

Significa que el archivo fue tomado y que el sistema pudo revisar los productos uno por uno.

Esto no quiere decir necesariamente que todos los productos hayan salido perfectos. Lo que significa es que el archivo pudo leerse correctamente y que el resultado está informado fila por fila.

En este archivo vas a ver estas columnas:

- `Fila Excel`
- `SKU`
- `Estado`
- `Producto`
- `Imagenes`
- `Mensaje`
- `Detalle`

## Cómo interpretar `Resultados`

### Estado `OK`

Quiere decir que ese producto salió bien.

En este caso, no hace falta hacer nada más para esa fila.

### Estado `PARTIAL_OK`

Quiere decir que el producto se cargó, pero hubo alguna observación puntual.

El ejemplo más frecuente es este:

- el producto se cargó;
- pero una imagen no pudo procesarse correctamente.

En este caso, te conviene revisar:

- las columnas `Imagenes`, `Mensaje` y `Detalle`.

Si querés corregirlo, podés volver a subir ese producto con la información ajustada.

### Estado `ERROR`

Quiere decir que ese producto no pudo cargarse correctamente.

En este caso te recomendamos revisar:

- `Mensaje`;
- `Detalle`.

Normalmente ahí vas a encontrar una pista concreta del problema, por ejemplo:

- un dato obligatorio faltante;
- un número mal escrito;
- un `SKU` incorrecto;
- una URL de imagen mal cargada.

Cuando corrijas ese dato, podés volver a subir el producto.

## Cómo conviene leer el archivo `Resultados`

La forma más práctica de revisarlo es esta:

1. mirá primero la columna `Estado`;
2. si dice `OK`, esa fila ya quedó bien;
3. si dice `PARTIAL_OK`, revisá especialmente imágenes, mensaje y detalle;
4. si dice `ERROR`, revisá el motivo, corregí esa fila y volvé a subirla.

En resumen:

- `OK` = todo bien;
- `PARTIAL_OK` = cargó, pero hay algo para revisar;
- `ERROR` = esa fila necesita corrección.

## Qué hacer si recibís `Resultados`

Te sugerimos este camino:

- abrí el archivo;
- revisá primero cuántas filas quedaron `OK`;
- después revisá si hay filas `PARTIAL_OK`;
- por último, mirá las filas `ERROR`;
- corregí solamente lo que lo necesite;
- y volvé a subir únicamente lo que quieras ajustar.

## Caso 2: recibís un archivo `ErroresEstructura`

Este archivo aparece cuando el problema no está en un producto puntual, sino en la estructura general del Excel.

En este caso, el archivo no pudo interpretarse correctamente como archivo de carga.

Por ejemplo, puede pasar si:

- falta una columna obligatoria;
- hay una columna repetida;
- el archivo no tiene la forma correcta;
- se modificó la estructura original de la plantilla.

## Qué significa `ErroresEstructura`

Si recibís este archivo, lo importante es entender esto:

- no hace falta revisar producto por producto;
- primero tenés que corregir la estructura del Excel;
- después volver a subir el archivo.

## Qué hacer si recibís `ErroresEstructura`

Te recomendamos seguir estos pasos:

1. abrí el archivo adjunto;
2. revisá qué columna falta, cuál está repetida o qué parte de la estructura no coincide;
3. corregí la plantilla original;
4. guardá nuevamente el archivo en formato Excel (técnicamente, `.xlsx`);
5. volvé a subirlo.

## Revisión final antes de subir

Antes de subir el archivo, te sugerimos hacer este control rápido:

- confirmá que no falten columnas;
- revisá que el `SKU` esté bien escrito;
- verificá que los campos numéricos tengan números;
- comprobá que `STOCK` tenga enteros;
- si usás imágenes, probá que los links abran;
- si no sabés qué poner en un campo opcional, podés dejarlo vacío;
- si querés dar de baja un producto, cargalo con stock `0`;
- si querés quitar una oferta, volvé a subir el producto con `OFERTA` vacío.

---

## Consejos útiles

Para que la carga sea más simple, más ordenada y con menos correcciones, te compartimos estos consejos:

1. Usá el archivo completo para altas de productos.
2. Usá el archivo corto solo para cambios de stock.
3. Si querés dar de baja un producto, dejalo con stock `0`.
4. No cambies la estructura del Excel.
5. Escribí bien el `SKU` antes de subir el archivo.
6. En stock usá siempre números enteros.
7. En medidas cargá alto, ancho y largo en centímetros.
8. En peso cargá el valor en gramos.
9. En imágenes usá links completos y en el orden en que quieras que queden.
10. En categoría y subcategoría copiá exactamente lo que figura en Club Patagonia.
11. Si querés quitar una oferta, volvé a subir el producto con `OFERTA` vacío.
12. Guardá siempre el archivo final en formato Excel (técnicamente, `.xlsx`).
13. Antes de cada carga, revisá la plantilla y los datos principales.

---

## Cierre

Completar bien el archivo desde el inicio te va a ahorrar tiempo y correcciones después.

Si seguís esta guía y respetás la estructura del Excel, la carga va a ser mucho más simple, clara y ordenada.
