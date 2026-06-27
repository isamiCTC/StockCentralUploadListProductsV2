# Pruebas para hacer en testing

La idea es no complicarla y probar lo importante con 5 casos nomás.

Si pueden, usen el provider `10300`, que ya está habilitado.

La carpeta para dejar los archivos es:
`//192.168.1.141/Operaciones/ProcesosAutomáticos/StockCentralUploadListProductsV2/10300`

Si en vez de eso crean otro provider en `StockCentral_Testing` del server `CTC027`,
acuérdense de crear también su carpeta con el mismo `providerID`.

## Caso 1. Archivo corto normal

Armar un Excel corto con varias filas, por ejemplo 4 o 5 SKUs que ya existan,
todos con stock válido.

Qué mirar:
- que tome el archivo;
- que procese todas las filas bien;
- que salga `Resultados`;
- que el original pase a `processed`.

## Caso 2. Archivo corto mezclado

Armar un Excel corto con varias filas:
- una con stock normal;
- una con stock `0`;
- una con algún dato mal a propósito, si quieren probar error de fila.

Qué mirar:
- que el stock `0` se tome bien;
- que una fila mala no rompa todo el archivo;
- que el resultado final muestre qué quedó bien y qué quedó mal.

## Caso 3. Archivo completo para alta

Armar un Excel completo con varias filas nuevas.

Meter:
- al menos 1 fila totalmente bien;
- al menos 1 fila con subcategoría válida del marketplace de Club Patagonia;
- al menos 1 fila con imagen válida.

Qué mirar:
- que cree los productos;
- que tome bien las subcategorías de Club Patagonia;
- que suba las imágenes;
- que el resultado quede prolijo.

## Caso 4. Archivo completo para actualización

Armar un Excel completo con varios SKUs que ya existan.

Cambiar cosas como:
- precio;
- stock;
- descripción;
- imagen.

Qué mirar:
- que actualice bien;
- que si una imagen falla no tire abajo todo;
- que si una fila tiene problema quede parcial o en error, pero no frene las otras.

## Caso 5. Archivo roto de estructura

Tomar cualquier Excel y romperle algo del formato, por ejemplo:
- borrar una columna obligatoria;
- cambiarle un encabezado;
- duplicar un encabezado.

Qué mirar:
- que no procese filas;
- que genere `ErroresEstructura`;
- que no genere `Resultados`;
- que llegue el adjunto correcto si el mail está activo.

## Con esto ya alcanza

Si estos 5 casos dan bien, ya deberían quedar probados los caminos más
importantes:
- stock;
- stock `0`;
- altas;
- updates;
- subcategorías;
- imágenes;
- errores por fila;
- errores de estructura;
- y cierre de archivo con adjuntos.
