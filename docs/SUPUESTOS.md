# Supuestos y decisiones tomados en la beta

Derivados de la sección 15 del plan (valores por defecto aplicados) y de vacíos detectados durante la implementación. No bloquean la beta; cualquier cambio con La Ingrata debe revalidar estas decisiones.

## Decisiones del plan (sección 15)

| Decisión | Valor aplicado |
| --- | --- |
| Unidades fraccionarias | No. Cantidades siempre enteras (`CHECK Cantidad > 0`). |
| Inventario por barra | No. La barra es solo texto libre en `Destino` del movimiento. |
| Quién autoriza mermas | Ambos roles por defecto, configurable con `App__PermitirMermaAlmacenista` (beta: `true`). |
| Expiración de sesión | 30 min (`Jwt__ExpireMinutes`), sin control de inactividad por activity. |
| Inventario inicial | Se carga como movimiento de tipo Entrada con referencia «saldo inicial». |
| Zona horaria | `America/Mexico_City` (`App__TimeZone`), configurable; fechas de negocio se muestran en local, se almacenan en UTC. |
| Hosting y certificados | Sin definir; desarrollo con `ng serve` y HTTP. Producción deberá definir HTTPS, dominio y certificados. |

## Supuestos funcionales

1. **Contraseña mínima:** 8 caracteres, con mayúscula, minúscula, número y símbolo (validación en API; Angular solo ayuda).
2. **Hash:** PBKDF2 (SHA-256, 100 000 iteraciones, sal aleatoria de 16 bytes), formato `PBKDF2|iteraciones|sal|hash`.
3. **`Identificador` de usuario es inmutable** después del alta (no se expone endpoint para cambiarlo).
4. **Protecciones de cuentas:** no se puede desactivar la propia cuenta ni al último administrador activo (409).
5. **Totales del dashboard:** netos; una reversión descuenta del tipo original (entrada anulada reduce `Entradas`, entrega anulada reduce `Entregas`).
6. **Filtro `hasta`:** con hora 00:00 se interpreta como fin del día (incluye todo el día).
7. **Sin paginación** en usuarios, marcas, categorías y productos (listas completas); historial de movimientos y bitácora sí paginan (20 por página).
8. **Bitácora:** solo consumible por API (`GET /api/bitacora`); la beta no incluye pantalla de auditoría en Angular.
9. **Roles:** únicamente `Administrador` (Id 1) y `Almacenista` (Id 2); se insertan con `INSERT` idempotente.
10. **Alta de productos:** existencia inicial 0; solo se mueven con movimientos registrados.
11. **Edición de producto:** nombre, marca y categoría son editables; SKU y unidad no se pueden cambiar si existe historial de movimientos (`MODIFICACION_PROHIBIDA`).
12. **Idempotencia:** un reintento con el mismo `requestId` devuelve el movimiento original (no duplica).
13. **Anulación:** solo Administrador, una sola vez por movimiento, y no se puede anular una anulación.
14. **Contraseñas:** nunca se almacenan ni transmiten en claro; no hay «olvidé mi contraseña» en la beta (queda fuera de alcance: se crea/ajusta cuenta por API con rol Admin).

## Supuestos técnicos

1. **Puerto de la API:** `8090` (`API_PORT`), porque el 8080 del equipo de desarrollo estaba ocupado por otro contenedor.
2. **CORS:** un único origen en desarrollo (`CORS_ORIGIN=http://localhost:4200`).
3. **`QUOTED_IDENTIFIER`:** los scripts se ejecutan con `SET QUOTED_IDENTIFIER ON` y sqlcmd con `-I`; la base se crea con `ALTER DATABASE ... SET QUOTED_IDENTIFIER ON`. Es obligatorio por los índices únicos filtrados (sección 7 del plan).
4. **Semilla:** `004_seed.sql` solo inserta roles y catálogo ficticio de desarrollo; **no** crea usuarios con contraseña por defecto (RD-05 se resuelve con el bootstrap de la API).
5. **Pruebas:** todos los PT corren contra SQL Server real vía Testcontainers (una instancia por suite), no con mocks.
6. **Logs de errores de aplicación:** el middleware central registra excepciones 5xx con `ILogger`; los mensajes al cliente no incluyen trazas.
7. **Sin migraciones EF Core:** el esquema lo gobiernan los scripts SQL idempotentes (`db/`); EF solo se usa para lecturas/escrituras simples de catálogo.

## Pendientes explícitamente fuera de la beta

- POS/comandas, facturación, contabilidad, múltiples almacenes, unidades fraccionarias, conteos físicos, código de barras, offline.
- Pruebas de carga (RNF-01 a RNF-03), disponibilidad 99 % (RNF-09), respaldos automáticos (RNF-10).
- Certificados/dominio en producción y definición de hosting.
