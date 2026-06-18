# Dudas a Plantear

## Objetivo de este documento

Este archivo junta dudas funcionales o de alcance que conviene validar antes de seguir implementando cambios en la V2.

La idea es separar:

- lo que hoy sabemos por código;
- lo que hacía o mostraba el legacy;
- y lo que todavía requiere una definición funcional.

---

## Duda 1: `FECHA DE INICIO` y `FECHA DE FIN`

### Lo que vemos hoy

En el archivo full import siguen existiendo estas columnas:

- `FECHA DE INICIO`
- `FECHA DE FIN`

En el legacy:

- se validan como parte obligatoria del header del formato de 19 columnas;
- aparecen variables `fechaInicio` y `fechaFin` dentro del flujo;
- pero no encontramos evidencia de que esas fechas entren en el request principal del proceso de importación de productos;
- tampoco encontramos que se lean efectivamente desde las columnas 17 y 18 para usarlas en ese flujo.

O sea:

- hoy parecen ser columnas **presentes y exigidas**;
- pero **no conectadas** al request principal del proceso.

### Evidencia encontrada en legacy

Validación de header:

- `FECHA DE INICIO` en `Rows[0][17]`
- `FECHA DE FIN` en `Rows[0][18]`

Referencias:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:934)
- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:936)

Variables declaradas en el flujo:

- `DateTime fechaInicio;`
- `DateTime fechaFin;`

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:1104)

Armado del payload principal del producto:

- no incluye esas fechas;
- arranca en el bloque donde se construye `ProductApi`.

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:1127)

### Métodos del legacy relacionados con fechas/precios especiales

Además del flujo principal de importación, en el legacy existen métodos separados para precio especial.

### 1. `SetEspecialPrice(string token, int Store, string sku, string Price)`

Qué hace:

- hace `POST` a `/rest/V1/products/special-price`

Payload esperado:

```json
{
  "prices": [
    {
      "price": <Price>,
      "store_id": <Store>,
      "sku": "<sku>",
      "price_from": "2021-11-01 00:00:00",
      "price_to": "2031-12-31 00:00:00",
      "extension_attributes": {}
    }
  ]
}
```

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:438)

### 2. `SetEspecialPriceAlt(string token, int Store, string sku, string Price, string StoreCode)`

Qué hace:

- hace `POST` a `/rest/{StoreCode}_store_view/V1/products/special-price`

Payload esperado:

```json
{
  "prices": [
    {
      "price": <Price>,
      "store_id": 0,
      "sku": "<sku>",
      "price_from": "2022-07-22 00:00:00",
      "price_to": "2031-12-31 00:00:00"
    }
  ]
}
```

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:451)

### 3. `EspecialPriceDelete(string token, int Store, string sku, string Price, string StoreCode, string price_from, string price_to)`

Qué hace:

- hace `POST` a `/rest/V1/products/special-price-delete`

Payload esperado:

```json
{
  "prices": [
    {
      "price": <Price>,
      "store_id": <Store>,
      "sku": "<sku>",
      "price_from": "<price_from>",
      "price_to": "<price_to>"
    }
  ]
}
```

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:465)

### Aclaración importante

Hasta donde vimos:

- estos métodos existen en el legacy;
- pero no encontramos evidencia de que formen parte del flujo activo del importador de productos que estamos replicando en V2.

Eso hace que hoy la duda no sea técnica sino funcional.

### Pregunta a validar

Pregunta sugerida:

> Hoy el archivo full import sigue trayendo `FECHA DE INICIO` y `FECHA DE FIN`. En el legacy vemos que esas columnas se validaban, y además existen métodos separados para precios especiales con payloads que usan `price_from` y `price_to`, pero no aparecen conectados al flujo principal de importación. ¿Eso está bien así y se deja igual en V2, o en realidad falta implementar esta parte para que esas fechas impacten algún request?

### Decisión pendiente

Hay que definir una de estas dos opciones:

### Opción A

Las fechas siguen en el archivo por compatibilidad, control o herencia, pero no deben impactar ninguna API.

Consecuencia para V2:

- se pueden seguir leyendo o validando;
- pero no hace falta conectarlas a ningún request.

### Opción B

Las fechas realmente deberían gobernar una lógica de precio especial o vigencia comercial.

Consecuencia para V2:

- habría que modelar explícitamente esa funcionalidad;
- definir en qué momento del flujo corre;
- definir si usa `SetEspecialPrice`, `SetEspecialPriceAlt`, `EspecialPriceDelete` o un equivalente nuevo;
- y decidir cómo se integra con `OFERTA`, `Price`, `ListPrice` y el resto del payload.

### Recomendación

Antes de implementar nada en V2 sobre estas columnas, conviene cerrar esta duda con definición funcional explícita.

Si no se valida, hay riesgo de:

- implementar una lógica que en realidad nunca se usó;
- o al revés, dejar afuera una parte de negocio que sí debía existir.

---

## Duda 2: `TIPO` y `AHORA`

### Lo que vemos hoy

En el archivo full import siguen existiendo estas columnas:

- `TIPO`
- `AHORA`

En el legacy:

- se validan como parte obligatoria del header del formato de 19 columnas;
- aparecen documentadas dentro del layout esperado;
- pero no encontramos evidencia de que alimenten el payload principal del proceso;
- y tampoco aparecen como campos del modelo `ProductApi` usado para requests a la API de productos.

O sea:

- hoy parecen ser columnas **presentes y exigidas**;
- pero **sin conexión visible** con los requests detectados del proceso.

### Evidencia encontrada en legacy

Validación de header:

- `TIPO` en `Rows[0][11]`
- `AHORA` en `Rows[0][12]`

Referencias:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:922)
- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:924)

Comentario del layout:

- aparece una referencia a `AHORA` como parte del formato esperado.

Referencia:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:889)

Modelo principal de request a productos:

- `ProductApi` no tiene campos `Tipo` ni `Ahora`.

Referencia:

- [Legacy/MG2Connector/Product.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Product.cs:8)

Armado de requests principales:

- en los `new ProductApi()` detectados no aparecen esas columnas cargadas como parte del payload.

Referencias:

- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:1127)
- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:1540)
- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:1974)
- [Legacy/MG2Connector/Magento.cs](/abs/c:/Users/isami/Desktop/QUERIES_APPS/APPS/StockCentralUploadListProductsV2/Legacy/MG2Connector/Magento.cs:7274)

### Pregunta a validar

Pregunta sugerida:

> Hoy el archivo full import sigue trayendo `TIPO` y `AHORA`. En el legacy vemos que esas columnas se validaban como parte del formato, pero no aparecen conectadas al payload principal de productos ni a requests detectados del proceso. ¿Se están usando realmente para algo fuera de lo que hoy vemos, o se pueden sacar del archivo directamente?

### Decisión pendiente

Hay que definir una de estas dos opciones:

### Opción A

`TIPO` y `AHORA` no tienen uso real en el proceso actual.

Consecuencia para V2:

- se pueden dejar solo por compatibilidad temporal;
- o directamente sacarlas del layout si negocio confirma que no sirven.

### Opción B

`TIPO` y `AHORA` sí tienen un significado funcional que hoy no está modelado o no quedó visible en el código que revisamos.

Consecuencia para V2:

- habría que definir con precisión qué comportamiento gobiernan;
- en qué request o subflujo impactan;
- y cómo se traducen a payloads o decisiones del proceso.

### Recomendación

Conviene validar esto con negocio o con quien conozca mejor el legacy operativo real antes de simplificar el layout del archivo.

Si no se valida, hay dos riesgos:

- mantener columnas “fantasma” que agregan ruido y confusión;
- o eliminar columnas que en algún flujo lateral sí tenían un uso real.
