# AGENTS.md - Workbook y contrato de entrada

Instrucciones especializadas para tareas dentro de `internal/workbook`. Se aplican junto con el `AGENTS.md` de la raiz `New/go`; ante una contradiccion, prevalece el archivo raiz y la instruccion explicita del usuario.

## Mision

Proteger el contrato de entrada del batch: lectura del Excel, deteccion de formato, validacion estructural, normalizacion conservadora, mapeo de filas y errores previos a cualquier impacto externo.

## Responsabilidades

- lectura de la primera hoja de archivos `.xlsx`;
- formatos admitidos de 2 y 19 columnas;
- matching normalizado de headers;
- preservacion del numero real de fila de Excel;
- omision de filas completamente vacias;
- conversion y validacion de SKU, stock, precios, medidas, peso, IVA y URLs;
- produccion de `MappedRow` e issues comprensibles para reporting;
- tests unitarios del paquete y documentacion del contrato de carga.

## Invariantes

- No agregar silenciosamente formatos nuevos ni compatibilidad con el formato legacy de 5 columnas.
- No deformar valores comerciales durante la normalizacion de celdas: cualquier transformacion nueva debe ser explicita y probada.
- Una fila con errores debe conservar, cuando sea posible, SKU y numero de fila para que pueda identificarse en resultados.
- Las validaciones deben ocurrir antes de llamar servicios externos.
- `STOCK` sigue siendo entero; `PESO` se recibe en gramos y su conversion operativa pertenece al flujo establecido.
- Todo cambio en caracteres permitidos para SKU debe incluir casos validos, invalidos, espacios externos e internos, y revisar su uso posterior en rutas HTTP.

## Limites y coordinacion

- No modificar clientes HTTP, SQL ni SendGrid sin coordinacion con el especialista de integraciones.
- No decidir estados finales `OK`, `PARTIAL_OK` o `ERROR`; esa semantica pertenece a batch/reporting.
- Si cambia una estructura consumida por `internal/batch`, avisar al orquestador antes de editar archivos de batch.
- Si cambia una regla visible para sellers, señalar las secciones afectadas de `docs/GUIA_CARGA_ARCHIVO_SELLERS*.md` y de la documentacion funcional.

## Validacion

- Agregar o ajustar tests cerca de `mapper_test.go`, `validator_test.go` o el archivo equivalente.
- Ejecutar como minimo `go test ./internal/workbook/...` desde `New/go`.
- Informar al orquestador los casos de borde cubiertos y cualquier contrato transversal que requiera pruebas adicionales.
