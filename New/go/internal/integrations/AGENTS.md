# AGENTS.md - Integraciones externas

Instrucciones especializadas para tareas dentro de `internal/integrations`. Se aplican junto con el `AGENTS.md` de la raiz `New/go`.

## Mision

Mantener contratos externos precisos, testeables y seguros para Products API, SQL Server, SendGrid y cualquier integracion incorporada al batch.

## Subareas

- `productsapi`: productos, stock, imagenes, DTO, envelopes, rutas HTTP y retry por deadlock.
- `sqlserver`: providers, ramas de categorias, stored procedures y mapeo de resultados.
- `sendgrid`: armado y envio del correo mediante el contrato de notificaciones.

## Invariantes de Products API

- Producto completo: intentar `PUT` y usar `POST` solo cuando la API indique `Producto inexistente`.
- Stock: hacer `GET`, reemplazar `Stock` y hacer `PUT`; no crear ni convertirlo en patch parcial.
- Imagenes: comparar por indice, intentar `PUT` y hacer `POST` solo ante `Imagen inexistente`.
- Reintentar unicamente el deadlock SQL reconocido y respetar contexto, timeout y configuracion de intentos.
- Conservar metadata de intentos y respuesta necesaria para diagnostico.
- Todo valor dinamico usado en una ruta, especialmente SKU, debe revisarse frente a reglas de encoding y tests de URL.

## Invariantes de SQL y correo

- No cambiar nombres de stored procedures, parametros o columnas por inferencia.
- La clasificacion usa la cache SQL precargada y el fallback configurado; no reintroducir endpoints legacy sin decision explicita.
- Los destinatarios se recortan, deduplican case-insensitive y ordenan.
- Un fallo de correo se registra pero no revierte el resultado funcional del archivo.

## Seguridad y limites

- Nunca imprimir ni copiar secretos de `config/.env`.
- No llamar servicios reales, SQL Server ni SendGrid durante desarrollo o pruebas salvo pedido explicito y alcance confirmado.
- Usar `httptest`, stubs y configuracion aislada.
- No trasladar decisiones de negocio a los clientes: exponer errores y metadata para que batch/reporting decidan.
- Coordinar cambios de DTO con todos sus consumidores antes de editar paquetes externos al alcance.

## Validacion

- Ejecutar tests del subpaquete afectado, por ejemplo `go test ./internal/integrations/productsapi/...`.
- Cubrir status HTTP, errores de transporte, envelopes de negocio y cantidad de intentos cuando aplique.
- Para cambios de rutas, afirmar en tests el path escapado que recibe el servidor.
- Informar contratos modificados, compatibilidad legacy y riesgos operativos al orquestador.
