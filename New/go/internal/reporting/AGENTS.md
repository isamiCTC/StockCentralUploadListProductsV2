# AGENTS.md - Reporting y comunicacion

Instrucciones especializadas para `internal/reporting` y, cuando el orquestador lo indique expresamente, para los paquetes hermanos `internal/results` e `internal/notifications`. Se aplican junto con el `AGENTS.md` de la raiz `New/go`.

## Mision

Convertir hechos tecnicos del procesamiento en estados, mensajes, archivos de salida y notificaciones consistentes, accionables y fieles a lo que realmente ocurrio.

## Responsabilidades

- estados por fila `OK`, `PARTIAL_OK` y `ERROR`;
- resultado separado de producto e imagenes;
- mensajes y detalles orientados a operacion;
- Excel `Resultados` y `ErroresEstructura`;
- adjuntos y composicion de notificaciones;
- coherencia entre salida, logs y documentacion funcional.

## Invariantes

- No informar `OK` si existe un fallo funcional pendiente en la fila.
- Si el producto fue impactado y fallan imagenes o una etapa posterior, preservar ese hecho y evaluar `PARTIAL_OK`.
- Una caida al fallback de categoria no convierte por si sola la fila en `ERROR`; si el producto se impacto, se informa como resultado parcial con observacion.
- Mantener el orden de resultados por fila de Excel y omitir filas completamente vacias.
- No perder SKU, numero de fila, etapa ni detalle util al traducir errores.
- Los archivos de salida deben conservar los nombres y columnas documentados salvo cambio de contrato explicitamente aprobado.
- Los errores de notificacion no deben reescribir el resultado funcional del archivo.

## Ownership y coordinacion

- Reporting decide la traduccion de hechos a estados; batch es quien produce y coordina esos hechos.
- `internal/results` es propietario de la escritura de Excel, no de reglas nuevas de negocio.
- `internal/notifications` compone destinatarios y mensajes; el envio concreto pertenece a integraciones.
- Coordinar con workbook cualquier cambio en issues estructurales o de fila.
- Coordinar con integraciones cualquier cambio en la interpretacion de respuestas externas.
- La documentacion para sellers debe usar lenguaje operativo y no exponer detalles internos innecesarios.

## Validacion

- Ejecutar los tests de `internal/reporting`, `internal/results` y `internal/notifications` que correspondan.
- Cubrir combinaciones de producto exitoso/fallido, imagenes parciales, fallback y errores estructurales cuando cambie la clasificacion.
- Verificar contenido y columnas del Excel mediante tests, no solo que el archivo pueda crearse.
- Informar al orquestador cualquier cambio observable en estados, mensajes, adjuntos o destinatarios.
