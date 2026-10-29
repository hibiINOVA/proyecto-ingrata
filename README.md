# Inventario Ingrata — Beta

Sistema de inventario para bodega y barra: catálogo, movimientos de almacén (entradas, entregas, mermas), anulaciones por reversión, dashboard de existencias e historial inmutable.

**Stack:** .NET 8 (API REST) · SQL Server 2022 · Angular 19 · Docker Compose · xUnit + Testcontainers.

## Requisitos

- Docker con Compose (motor en ejecución)
- Para pruebas: .NET SDK 8
- Para desarrollo del frontend: Node.js 20+

## Arranque verificado

```bash
# 1. Variables de entorno (NUNCA versiones .env)
cp .env.example .env
# Edita .env: MSSQL_SA_PASSWORD, ConnectionStrings__Default (misma contraseña),
# Jwt__Key (≥32 caracteres), BOOTSTRAP_ADMIN_ID y BOOTSTRAP_ADMIN_PASSWORD

# 2.Levantar base de datos y API
docker compose up -d

# 3. Verificar
curl http://localhost:8090/health
# {"estado":"ok","fechaUtc":"..."}

# 4. Login de prueba (usa las credenciales de BOOTSTRAP_ADMIN_* en .env)
curl -X POST http://localhost:8090/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"identificador":"admin","password":"<BOOTSTRAP_ADMIN_PASSWORD>"}'
```

- **API:** `http://localhost:8090` (variable `API_PORT` en `.env`; 8090 por defecto porque 8080 suele estar ocupado)
- **SQL Server:** `localhost:1433` (solo para desarrollos locales; la API usa el host `db`)

### Alta inicial del Administrador (RD-05)

Al arrancar, si la base no tiene ningún usuario con rol Administrador, la API crea la cuenta con `BOOTSTRAP_ADMIN_ID` / `BOOTSTRAP_ADMIN_PASSWORD` y la registra en la bitácora. Si faltan las variables, no se crea ninguna cuenta y se registra una advertencia en el log. **No existe usuario con contraseña por defecto en los scripts de base de datos.**

### Frontend (desarrollo)

```bash
cd frontend
npm install
npm start          # http://localhost:4200 (CORS ya permite este origen en .env)
```

La pantalla de login acepta cualquier identificador/contraseña válidos de la API (p. ej. el administrador bootstrap).

### Pruebas automatizadas

```bash
cd backend
dotnet test        # requiere Docker: levanta SQL Server 2022 con Testcontainers
```

Cobertura: PT-01, PT-02, PT-03, PT-06, PT-07, PT-10 a PT-18, PT-21, PT-28, PT-38, idempotencia por `requestId` y escenarios HU-04, HU-05, HU-07, HU-08, HU-18, HU-19 a HU-21 y bitácora (sección 12 del plan).

## Estructura

```
├── docker-compose.yml      # db (SQL Server) + api (.NET), healthchecks
├── .env.example            # plantilla de configuración (sin secretos)
├── db/                     # scripts idempotentes aplicados al iniciar el contenedor
│   ├── 001_schema.sql      # tablas, índices (incl. únicos filtrados), constraints
│   ├── 002_procedures.sql  # sp_RegistrarMovimiento / sp_AnularMovimiento (transaccionales)
│   ├── 003_triggers.sql    # inmutabilidad de Movimiento y Bitacora
│   └── 004_seed.sql        # roles + catálogo ficticio (sin usuarios)
├── backend/
│   ├── src/Ingrata.Api/    # Auth, Catalog, Movements, Dashboard + Shared
│   └── tests/Ingrata.Tests # integración con SQL Server real (Testcontainers)
├── frontend/               # Angular 19 standalone
└── docs/                   # SUPUESTOS.md y REPORTE_BETA.md
```

## Reglas clave (garantizadas en la base de datos)

- Saldo nunca negativo (`CHECK` + `UPDATE` condicional dentro del SP).
- Atomicidad de cada movimiento (SP con `BEGIN TRAN` / `XACT_ABORT`).
- Movimientos y bitácora inmutables (`INSTEAD OF UPDATE/DELETE`).
- Una sola anulación por movimiento e idempotencia por `requestId` (índices únicos filtrados).

> Nota técnica: los scripts se ejecutan con `SET QUOTED_IDENTIFIER ON` (y sqlcmd con `-I`) porque los índices únicos filtrados lo exigen tanto al crear los objetos como al insertar filas.

## Códigos de error

400 validación · 401 sin token · 403 sin permiso · 404 inexistente · 409 conflicto (`SALDO_INSUFICIENTE`, `YA_ANULADO`, `MODIFICACION_PROHIBIDA`, `CONFLICTO`) · 500 error interno. Cuerpo: `{ "codigo": "...", "mensaje": "..." }`.
