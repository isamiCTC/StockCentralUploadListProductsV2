# AGENTS.md - Batch y procesamiento

Instrucciones especializadas para tareas dentro de `internal/batch`. Se aplican junto con el `AGENTS.md` de la raiz `New/go`.

## Mision

Preservar el flujo one-shot completo y la independencia logica de cada archivo y SKU, coordinando workbook, catalogo, Products API, imagenes, resultados y notificaciones sin mezclar responsabilidades.

## Responsabilidades

- escaneo y procesamiento secuencial de archivos;
- transiciones controladas entre `input`, `processing` y `processed`;
- validacion estructural y despacho por tipo de formato;
- worker pool, contextos y timeout por fila;
- procesamiento de stock y carga completa;
- coordinacion del upsert, clasificacion e imagenes;
- ordenamiento final por numero de fila;
- construccion de evidencia suficiente para reporting y logging.

## Invariantes

- El proceso corre una vez y termina: no introducir scheduler, loop infinito ni reintento global persistente.
- Los archivos se procesan de a uno; las filas pueden procesarse concurrentemente.
- Un error de una fila no invalida automaticamente las demas filas de un archivo estructuralmente valido.
- Cada SKU debe mantener su contexto, timeout y bloque de log independiente.
- No intercalar bloques del log `detail` de SKUs distintos.
- Un timeout posterior al impacto del producto puede producir `PARTIAL_OK`; no perder evidencia de efectos ya realizados.
- El provider operativo proviene de la carpeta numerica valida.

## Ownership y coordinacion

- Batch es propietario de la orquestacion, no de las reglas detalladas de Excel ni de los protocolos externos.
- Cambios en `internal/workbook` requieren coordinacion con el especialista de workbook.
- Cambios en clientes o DTO externos requieren coordinacion con integraciones.
- Cambios en estados y mensajes requieren coordinacion con reporting.
- No ejecutar el comando real `run` ni mover archivos operativos durante pruebas.

## Validacion

- Preferir stubs, `httptest`, contextos cancelables y `t.TempDir()`.
- Ejecutar como minimo `go test ./internal/batch/...` y los paquetes directamente afectados.
- Incluir tests de error parcial, timeout o concurrencia cuando el cambio altere esos caminos.
- Entregar al orquestador un resumen de efectos externos posibles y pruebas ejecutadas.
