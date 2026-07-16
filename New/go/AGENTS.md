# AGENTS.md

Guia persistente para agentes que trabajen dentro de `New/go`.

Este proyecto es la V2 en Go de `StockCentralUploadListProductsV2`, un batch one-shot para procesar Excels de productos, impactar la API de productos, generar resultados y notificar por mail.

## Prioridad de contexto

Cuando una instruccion de usuario, este archivo y la documentacion entren en tension, usar este orden:

1. Instruccion explicita del usuario en la conversacion actual.
2. Comportamiento real implementado en codigo.
3. `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md` como fuente documental principal del comportamiento actual.
4. `docs/PROCESO_FUNCIONAL_CARGA_PRODUCTOS.md` para lenguaje funcional y operativo.
5. `docs/GUIA_CARGA_ARCHIVO_SELLERS.md` para lenguaje orientado a sellers.
6. `README.md`, scripts y tests como soporte operativo.

Si hay una diferencia entre docs y codigo, no inventar una reconciliacion silenciosa. Revisar el codigo, explicar la diferencia y, si la tarea lo permite, actualizar la documentacion afectada.

## Primeros archivos a leer

Para entender una tarea nueva, priorizar:

- `README.md`: vista rapida, comandos y estructura.
- `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md`: fuente de verdad funcional/tecnica actual.
- `docs/PROCESO_FUNCIONAL_CARGA_PRODUCTOS.md`: proceso explicado para negocio y operaciones.
- `docs/TESTS_UNITARIOS.md`: inventario de cobertura y forma oficial de correr tests.
- `config/appsettings.toml`: configuracion no sensible actual.
- `internal/app/runbatch/runtime.go`: armado de dependencias del batch.
- `internal/batch/processor.go`: orquestacion del batch completo.
- `internal/batch/file_processor.go`: procesamiento de un archivo y sus filas.
- `internal/workbook/*`: lectura, validacion y mapeo de Excel.
- `internal/integrations/productsapi/*`: cliente de API de productos, upsert e imagenes.
- `internal/integrations/sqlserver/*`: acceso a SQL Server para providers y categorias.
- `internal/integrations/sendgrid/*`: cliente concreto de correo.
- `internal/catalog/*`: resolucion de categoria/subcategoria.

No leer todo el repo por inercia. Seguir las rutas anteriores segun el cambio pedido.

## Comandos oficiales

Ejecutar desde `New/go`.

### Tests

La forma oficial de correr la suite es:

```powershell
./scripts/test.ps1
```

Ese script ejecuta `go test -count=1 ./...`, muestra salida en vivo y cierra con resumen.

Usar `go test` directo solo para ciclos puntuales de desarrollo, por ejemplo:

```powershell
go test ./internal/workbook/...
go test ./internal/integrations/productsapi/...
go test ./internal/integrations/sqlserver/...
```

Antes de cerrar cambios de codigo Go, correr `./scripts/test.ps1` siempre que sea viable. Si no se puede correr, decirlo claramente y explicar por que.

### Build

Para compilar el artefacto:

```powershell
./scripts/build.ps1
```

El script genera `dist/<os>-<arch>/`, copia `config/` y usa caches locales en `.cache/` por defecto.

### Self-check y run

Estos comandos pueden tocar filesystem real, SQL Server y servicios externos. No ejecutarlos salvo pedido explicito o necesidad muy clara.

```powershell
go run ./cmd/StockCentralUploadListProductsV2 self-check --settings config/appsettings.toml --env config/.env
go run ./cmd/StockCentralUploadListProductsV2 run --settings config/appsettings.toml --env config/.env
```

## Directorios generados o no fuente

Tratar como salida generada o runtime:

- `.cache/`
- `dist/`
- `logs/`

No basar cambios de negocio en esos directorios. No limpiarlos ni borrarlos salvo pedido explicito.

## Configuracion y secretos

- `config/appsettings.toml` contiene configuracion no sensible.
- `config/.env` contiene secretos y no debe imprimirse, copiarse ni documentarse con valores reales.
- Variables sensibles esperadas: `DB_CONNECTION_STRING`, `PRODUCTS_API_TOKEN`, `SENDGRID_API_KEY`.
- Si una tarea requiere credenciales o servicios externos, preferir tests/stubs. Pedir confirmacion antes de ejecutar comandos que dependan de infraestructura real.

## Modelo operativo

El proceso V2 es un batch de una sola corrida:

- no es Windows Service;
- no tiene loop infinito;
- no hace scheduling interno;
- procesa archivos detectados y termina;
- deja la planificacion a un scheduler externo.

Preservar ese modelo. No introducir loops, timers internos o reintentos globales persistentes sin una decision explicita de producto/operacion.

## Flujo funcional esencial

El comportamiento esperado actual es:

1. La CLI recibe `run` o `self-check`.
2. `run` carga TOML y `.env`, inicializa logs y arma dependencias.
3. Se carga el mapping global de categorias desde SQL Server.
4. Se obtienen providers habilitados desde SQL Server.
5. El scanner busca `.xlsx` dentro de carpetas numericas de providers validos.
6. Los archivos se procesan de a uno.
7. Cada archivo se mueve de `input` a `processing`.
8. Se lee la primera hoja del Excel.
9. Se detecta formato por cantidad de columnas: 2 o 19.
10. Se valida estructura con matching laxo de headers.
11. Si falla estructura, se genera `ErroresEstructura`, se mueve a `processed` y se notifica.
12. Si la estructura es valida, se mapean filas.
13. Las filas se procesan concurrentemente con worker pool.
14. Cada fila es una transaccion logica independiente.
15. Se ordenan resultados por fila de Excel.
16. Se mueve el original a `processed`.
17. Se genera `Resultados`.
18. Se manda mail con el Excel de salida y el archivo procesado.

## Formatos de Excel

Solo se soporta `.xlsx`.

Formatos validos:

- 2 columnas: `SKU`, `STOCK`.
- 19 columnas: carga completa de producto.

El formato historico de 5 columnas no existe en V2.

Reglas importantes:

- El provider operativo sale de la carpeta numerica valida, no del Excel ni del nombre de archivo.
- Se usa la primera hoja.
- Headers se comparan con normalizacion laxa.
- Filas completamente vacias se omiten y no se escriben en resultados.
- `PESO` se informa en gramos y se envia en kilogramos.
- `STOCK` debe ser entero.
- `OFERTA > 0` pisa `Price` y conserva `ListPrice`/`NetPrice` desde precio base.
- `IVA` entre `0` y `1` se convierte a porcentaje.

## Categorias y subcategorias

Este es un punto delicado. No cambiarlo por intuicion.

Regla actual:

- `CATEGORIA` se exige y se conserva, pero la resolucion efectiva se apoya en `SUB CATEGORIA`.
- Primero se precarga desde SQL Server una cache de ramas validas del catalogo usando `CatalogCategoryBranchLookup_Get @CatalogoId = <catalog_id>`.
- La comparacion contra esa cache normaliza trim, mayusculas, tildes y espacios internos.
- Si no hay match en la cache, se usa el fallback configurado: `catalog.fallback_category_code` y `catalog.fallback_category_name`.
- En la configuracion actual el fallback documentado es `1041 / Varios`.

Caer en fallback no debe convertir la fila en `ERROR` por si solo. Si el producto se impacta, la fila queda `PARTIAL_OK` con observacion de categoria general.

### Diferencia con el legacy

- El legacy mezclaba mapping previo, endpoint de subcategorias y fallback final.
- La V2 actual ya no consulta el endpoint de subcategorias para clasificar.
- La V2 resuelve solo contra el catalogo SQL precargado del `catalog_id` configurado y, si no encuentra match, cae al fallback configurado.

Archivos clave:

- `internal/catalog/resolver.go`
- `internal/integrations/sqlserver/category_branch_repository.go`
- `internal/catalog/normalize.go`
- `internal/reporting/row_outcome_builder.go`

## API de productos

La semantica legacy se conserva.

Producto completo:

- Primero `PUT /providers/{providerID}/products/{sku}/`.
- Si la API responde `Producto inexistente`, entonces `POST /providers/{providerID}/products`.
- Otros errores de API hacen fallar la fila.

Stock:

- `GET /providers/{providerID}/products/{sku}/`.
- Pisar `Stock`.
- `PUT /providers/{providerID}/products/{sku}/`.
- No crea producto y no hace patch parcial.

Retry por interbloqueo:

- El `PUT` y el `POST` del upsert, y el `PUT` de stock, se reintentan solo si
  `Result.Description` contiene el mensaje específico de deadlock de SQL Server.
- La política usa `products_api.deadlock_max_attempts` y
  `products_api.deadlock_base_delay_ms`.
- Otros errores HTTP y errores de transporte no se reintentan.
- El backoff respeta el contexto y el timeout propio de la fila.
- `product-upsert-ok` informa `update_attempts` y `create_attempts`; si se
  agotan los intentos, el error informa el total y conserva la última respuesta.

Archivos clave:

- `internal/integrations/productsapi/client.go`
- `internal/integrations/productsapi/products.go`
- `internal/integrations/productsapi/dto.go`

## Imagenes

La sincronizacion de imagenes tambien conserva semantica legacy.

Reglas actuales:

- `URL IMAGENES` puede venir vacio.
- Si trae contenido no vacio y una URL es invalida, la fila queda en error antes de sincronizar imagenes.
- Multiples URLs se separan con `&`.
- El orden de URLs define el indice de imagen.
- Se descarga cada imagen y se convierte WebP a JPEG cuando corresponde.
- Primero se consulta la imagen existente por indice.
- Si el Base64 existente es igual, no se resube.
- Si cambia, intenta `PUT`.
- Si el `PUT` indica `Imagen inexistente`, hace `POST`.
- Si el producto se impacto pero una o mas imagenes fallan, la fila queda `PARTIAL_OK`.

Archivos clave:

- `internal/images/downloader.go`
- `internal/integrations/productsapi/images.go`
- `internal/reporting/row_outcome_builder.go`

## Resultados y mails

Resultados por fila:

- `OK`
- `PARTIAL_OK`
- `ERROR`

Excel `Resultados`:

- `Fila Excel`
- `SKU`
- `Estado`
- `Producto`
- `Imagenes`
- `Mensaje`
- `Detalle`

Excel `ErroresEstructura`:

- `Campo`
- `Mensaje`
- `Detalle`

Notificaciones:

- SendGrid es el sender concreto.
- Destinatarios = `always_recipients` + email del provider si existe.
- Se recortan espacios, se deduplican case-insensitive y se ordenan.
- Para `PROCESSED` y `PROCESSED_WITH_ERRORS`, se adjunta `Resultados` y el original ya movido a `processed`.
- Para `STRUCTURE_ERROR`, se adjunta `ErroresEstructura` y el original ya movido a `processed`.
- Fallos de SendGrid se registran, pero no revierten el resultado funcional del archivo.

Archivos clave:

- `internal/results/writer.go`
- `internal/reporting/*`
- `internal/notifications/*`

## Logging

Hay dos logs:

- `summary`: consola + archivo, seguimiento resumido.
- `detail`: archivo, seguimiento tecnico y fila por fila.

El `detail` por fila usa buffers para escribir bloques completos por SKU y evitar intercalado entre workers.

No reemplazar el logger por `fmt.Println` en codigo de proceso. Usar las abstracciones de `internal/logging`.

## Concurrencia y timeouts

- Los archivos se procesan secuencialmente.
- Las filas dentro de un archivo se procesan concurrentemente.
- `batch.row_workers` controla el worker pool.
- `batch.row_timeout_seconds` aplica por fila con contexto propio.
- Si una fila de stock excede timeout, queda `ERROR`.
- Si una fila full import excede timeout durante imagenes despues de impactar producto, puede quedar `PARTIAL_OK`.

Preservar la independencia entre filas. Un error de una fila no debe invalidar automaticamente el resto de un archivo estructuralmente valido.

## Estilo de codigo Go

- Mantener paquetes pequenos y responsabilidades actuales.
- Usar `context.Context` en operaciones que puedan bloquear o llamar servicios externos.
- Envolver errores con contexto usando `fmt.Errorf("...: %w", err)`.
- Evitar panics fuera de arranque/configuracion intencional.
- Mantener clientes externos detras de paquetes existentes (`products`, `providers`, `notifications`, etc.).
- Usar `httptest`, stubs y `t.TempDir()` en tests.
- No introducir dependencias nuevas sin una razon clara.
- Ejecutar `gofmt` sobre archivos Go modificados.

## Convenciones de cambios

Cuando modifiques comportamiento:

- Actualizar tests cerca del paquete afectado.
- Actualizar `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md` si cambia la fuente de verdad.
- Actualizar `docs/PROCESO_FUNCIONAL_CARGA_PRODUCTOS.md` si cambia el proceso observable por negocio/operaciones.
- Actualizar `docs/GUIA_CARGA_ARCHIVO_SELLERS.md` si cambia algo que debe saber el seller.
- Actualizar `docs/TESTS_UNITARIOS.md` si se agregan, eliminan o cambian tests relevantes.

Cuando modifiques solo documentacion:

- Cuidar que la guia para sellers no exponga detalles tecnicos innecesarios.
- Cuidar que el proceso funcional no contradiga `STOCKCENTRALUPLOADLISTPRODUCTSV2.md`.
- Mantener lenguaje claro y accionable.

## Seguridad operativa

No ejecutar comandos que:

- procesen archivos reales;
- muevan archivos entre `input`, `processing` y `processed`;
- llamen SQL Server real;
- llamen Products API real;
- manden mails reales;
- borren `dist`, `.cache`, `logs` u otros artefactos;

salvo que el usuario lo pida explicitamente o sea imprescindible y se haya explicado el alcance.

Para validar codigo, preferir unit tests y build.

## Reglas de documentacion para agentes

- No usar `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md` como lugar para propuestas futuras: describe lo que aplica hoy.
- Si aparece una duda o decision pendiente nueva, documentar la resolucion final en la documentacion vigente o en el sistema de seguimiento que corresponda.
- Si algo es guia operativa para sellers, usar `docs/GUIA_CARGA_ARCHIVO_SELLERS.md`.
- Si algo es contrato interno de proceso, usar `docs/PROCESO_FUNCIONAL_CARGA_PRODUCTOS.md`.
- Si algo es detalle implementado o trazabilidad tecnica, usar `docs/STOCKCENTRALUPLOADLISTPRODUCTSV2.md`.

## Checklist antes de cerrar una tarea

Antes de responder como terminado:

- Revisar `git diff` de los archivos tocados.
- Confirmar que no se tocaron cambios ajenos sin querer.
- Correr `gofmt` si hubo cambios Go.
- Correr `./scripts/test.ps1` si hubo cambios Go o de comportamiento.
- Si no se corrieron tests, dejarlo dicho.
- Mencionar los archivos principales modificados y el efecto real del cambio.

## Notas para futuras sesiones

Este archivo debe mantenerse vivo. Si durante una tarea aparece una regla recurrente, una confusion que se repite o una convencion que evitaria retrabajo, actualizar este `AGENTS.md` junto con el cambio correspondiente.
