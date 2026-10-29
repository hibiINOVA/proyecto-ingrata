# Reporte de la beta — Inventario Ingrata

Fecha: 2026-10-29 · Rama: `main` · Verificaciones ejecutadas por el agente en esta máquina.

## 1. Resumen

Beta completa de extremo a extremo: scripts de SQL Server idempotentes aplicados automáticamente al subir el contenedor, API .NET 8 con las 21 HU, frontend Angular 19 con las pantallas del alcance, Docker Compose (bd + api) y suite xUnit + Testcontainers con **29/29 pruebas en verde** (3 corridas consecutivas).

### Cómo verificar

```bash
docker compose up -d && curl http://localhost:8090/health   # API + BD
cd backend && dotnet test                                    # 29/29 (requiere Docker)
cd frontend && npm install && npm start                      # UI en :4200
```

## 2. Estado por historia de usuario

|HU / RF | Estado | Evidencia |
| --- | --- | --- |
| HU-01 Login válido | ✅ | PT-01 (prueba), smoke manual `POST /api/auth/login` |
| HU-02 Logout invalida el token | ✅ | PT-02 logout (revocación por `jti` + `SesionRevocada`) |
| HU-03 RBAC / 403 Almacenista | ✅ | PT-03 (catálogo, usuarios, anulación) y bitácora de rechazos |
| HU-04 Cuentas: alta, baja lógica, protección | ✅ | Pruebas HU-04 (desactivada pierde acceso; no auto-desactivación ni último admin) |
| HU-05 Nombres duplicados (marca/categoría) | ✅ | Prueba HU-05 (409) |
| HU-06 Catálogo productos (SKU único, obligatorios) | ✅ | PT-06 SKU duplicado / campos obligatorios |
| HU-07 Búsqueda y filtros | ✅ | Prueba HU-07 (texto, marca, categoría) |
| HU-08 Baja lógica de producto | ✅ | Prueba HU-08 (inactivo no acepta movimientos; reactivación) |
| HU-09 Edición de producto | ✅ | PT-07 (nombre editable conserva folios; unidad con historial → 409) |
| HU-10 a HU-12 Entradas, entregas y mermas | ✅ | PT-10 (ejemplo ERS 2+5=7 → entrega 2 ⇒ 5 → merma 1 ⇒ 4), PT-11/PT-12 inválidos, PT-12 (404/409) |
| HU-13 Saldo nunca negativo | ✅ | SP + `CHECK`; PT-12/PT-13/PT-16 (`SALDO_INSUFICIENTE`) |
| HU-14 Atomicidad | ✅ | PT-13 (concurrencia: exactamente una gana, saldo 0) y PT-14 (rollback tras UPDATE de saldo) |
| HU-15 Folios únicos/consecutivos | ✅ | Prueba HU-18 (secuencia `SeqFolio`) |
| HU-16 Anulación por reversión (solo Admin) | ✅ | PT-16 (repone saldo, segunda anulación 409, entrada consumida 409) y PT-03 |
| HU-17 Historial inmutable | ✅ | PT-15 (`UPDATE`/`DELETE` directos rechazados por triggers) |
| HU-18 Consistencia saldo = historial | ✅ | PT-17 (suma algebraica = `SaldoProducto.Existencia`) |
| HU-19 Dashboard de existencias | ✅ | Prueba HU-19/HU-21 + smoke manual |
| HU-20 Historial filtrable | ✅ | Prueba HU-20 (fechas, producto, tipo, usuario; 403 cruzado) |
| HU-21 Misma hora local en dos consultas | ✅ | PT-21 |

### Requisitos no funcionales y decisiones

| Req. | Estado | Evidencia |
| --- | --- | --- |
| RNF-04 hash con sal | ✅ | PBKDF2-SHA256 / 100k iteraciones / sal 16 B |
| RNF-05 expiración y permisos | ✅ | JWT 30 min, revocación en logout, cuentas desactivadas rechazadas |
| RNF-06 bitácora | ✅ | Prueba de bitácora (login, gestión de catálogo y eventos de acceso) — vía API |
| RNF-07 validación en servidor | ✅ | `FluentValidation`-style manual + `InvalidModelState` → `{codigo:"VALIDACION"}` |
| RNF-08 consistencia | ✅ | Reglas en BD: CHECK, SPs transaccionales, triggers, índices únicos |
| RNF-11 capas y Docker | ✅ | Clean-ish architecture por módulos + `docker compose up` |
| RNF-12 mensajes comprensibles | ✅ | Middleware central, sin trazas al cliente (PT-28/PT-38) |
| IU-01 a IU-03 (interfaz responsiva) | ✅ parcial | CSS móvil-primero con corte `min-width: 720px`; **verificación visual manual en 360 px/tableta pendiente** |
| IS-02 códigos HTTP | ✅ | PT-28/PT-38 (400/401/403/404/409) |
| RD-01 a RD-04 (reglas de datos) | ✅ | 001_schema.sql |
| RD-05 alta inicial Admin | ✅ | `SemillaAdminInicial` con variables de entorno; sin credenciales por defecto en la BD |

## 3. Resultados de pruebas

- **Suite:** `cd backend && dotnet test` → `Passed! Failed: 0, Passed: 29, Total: 29` (corridas consecutivas: 3 verdes tras los arreglos).
- **PT-13 y PT-14** (repetibles): incluidas en la suite; PT-13 confirma exactamente una entrega ganadora con saldo final 0; PT-14 confirma rollback completo.
- **Idempotencia `requestId`:** prueba propia en la suite.
- **Docker:** `docker compose up -d` → `ingrata-db` healthy (scripts 001-004 aplicados), `ingrata-api` arriba, `/health` 200.
- **Smoke manual (compose):** login admin → crear marca/categoría/producto → entrada 7 → entrega 2 → dashboard `entradas:7, entregas:2, existencias:5` (coherente con el ejemplo ERS).
- **Frontend:** `ng build` (desarrollo y producción) sin errores.

## 4. Problemas detectados y resueltos durante el desarrollo

1. **`QUOTED_IDENTIFIER` en índices únicos filtrados:** sqlcmd inicia con la opción en OFF; `CREATE INDEX` fallaba silenciosamente y luego los `INSERT` en `Movimiento` fallaban en runtime. Solución: `SET QUOTED_IDENTIFIER ON` al inicio de cada script, flag `-I` de sqlcmd, `ALTER DATABASE ... SET QUOTED_IDENTIFIER ON` al crear la base (entrypoint y Testcontainers).
2. **Respuesta de creación de movimiento sin `id`:** el SP solo devolvía folio; ahora el servicio resuelve el id por folio (necesario para anulaciones).
3. **Cadena de conexión en pruebas:** apuntaba a `master`; corregido a la base `Ingrata` en `IngrataApiFactory`.
4. **Expectativa del dashboard:** 8 entradas − 3 entregas − 1 merma = 4 existencias (la prueba esperaba 5); corregida la prueba.
5. **Puerto 8080 ocupado** en la máquina de desarrollo: la API quedó en 8090 (`API_PORT`).

## 5. Qué falta (no bloqueante)

- Verificación visual manual a 360 px y tableta (el CSS está preparado).
- Hosting, HTTPS y certificados en producción.
- Pantalla de bitácora/auditoría en Angular (por ahora solo endpoint).
- «Olvidé mi contraseña» / alta de usuarios sin API (fuera del alcance de la beta).
- Pruebas de carga, disponibilidad 99 % y respaldos automáticos (explícitamente fuera de alcance).
- Poblar la BD de demostración con datos de negocio reales (hasta ahora: catálogo semilla + datos de prueba).

## 6. Supuestos

Ver [docs/SUPUESTOS.md](SUPUESTOS.md) (sección 15 del plan resuelta con los valores por defecto + vacíos detectados).
