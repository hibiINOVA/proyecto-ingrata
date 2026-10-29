# Instrucciones para el agente: construir la versión beta de Inventario Ingrata

**Proyecto:** Plataforma web de control de inventarios para La Ingrata
**Equipo:** Bajío Stack (Instituto Tecnológico Sanmiguelense de Estudios Superiores [ITSES])
**Fuentes de verdad:** ERS v0.1 (ITSES, 2026a), Modelado formal v1.0 (ITSES, 2026d), HU/BDD y RTM (ITSES, 2026b), Arquitectura y clases (ITSES, 2026e), Memoria de estimación y cronograma (ITSES, 2026c). Las referencias completas están al final.

---

## 1. Misión

Construye de extremo a extremo la **primera versión beta** del sistema: API REST, base de datos, interfaz web responsiva y contenedores Docker. La beta debe permitir que el personal de almacén inicie sesión, consulte el catálogo, registre entradas, entregas a barra y mermas con consistencia garantizada, anule movimientos (solo Administrador) y consulte existencias e historial.

Entrega código que compile, corra con `docker compose up` y pase las pruebas descritas en la sección 11. No inventes requisitos: si algo no está en este documento ni en las fuentes, elige la opción más simple, regístrala en `docs/SUPUESTOS.md` y continúa.

## 2. Reglas de trabajo para el agente

1. Trabaja por fases (sección 9) y en orden. Al terminar cada fase: compila, corre pruebas, haz commit con mensaje convencional (`feat:`, `test:`, `chore:`).
2. Nunca pongas secretos, contraseñas ni cadenas de conexión reales en el repositorio. Todo va en variables de entorno (`.env.example` sí se versiona; `.env` no).
3. Toda regla de negocio crítica (saldo no negativo, atomicidad, inmutabilidad, una sola anulación) se aplica **en la base de datos**, no solo en la API ni en la interfaz.
4. La API valida siempre; Angular solo ayuda al usuario. Asume que cualquier cliente puede llamar la API directamente.
5. No agregues funciones fuera de alcance (sección 3.2). Si dudas, no lo hagas.
6. Mantén un `README.md` con pasos de arranque verificados por ti mismo.
7. Al final, entrega un reporte `docs/REPORTE_BETA.md` con: qué quedó hecho por HU, qué falta, supuestos tomados y resultados de pruebas.

## 3. Alcance de la beta

### 3.1 Dentro del alcance

Las 21 historias de usuario (HU-01 a HU-21), implementadas en su versión mínima funcional:

| Módulo | HU / RF | Entregable mínimo |
| --- | --- | --- |
| Autenticación y usuarios | HU-01 a HU-04 / RF-01 a RF-04 | Login JWT, logout con revocación, RBAC en API, alta y baja lógica de cuentas |
| Catálogo | HU-05 a HU-09 / RF-05 a RF-09 | CRUD de marcas, categorías y productos; baja lógica; búsqueda y filtros |
| Movimientos | HU-10 a HU-18 / RF-10 a RF-18 | Entrada, entrega, merma, anulación por reversión, existencias derivadas, historial inmutable |
| Dashboard y consultas | HU-19 a HU-21 / RF-19 a RF-21 | Existencias actuales, totales por periodo, historial filtrable, hora local |

Requisitos no funcionales a cumplir en la beta: RNF-04 (hash con sal), RNF-05 (expiración y permisos), RNF-06 (bitácora), RNF-07 (validación en servidor), RNF-08 (consistencia), RNF-11 (separación de capas y Docker), RNF-12 (mensajes de error comprensibles), IU-01 a IU-03, IS-02 (códigos HTTP), RD-01 a RD-05.

### 3.2 Fuera del alcance (no implementar)

Integración con POS o comandas, contabilidad, facturación, pagos, apps nativas, múltiples almacenes, inventario independiente por barra, unidades fraccionarias, ajustes por conteo físico, lectura de códigos de barras, funcionamiento sin conexión. Tampoco: pruebas de carga formales (RNF-01 a RNF-03), disponibilidad 99 % (RNF-09) y respaldos automáticos (RNF-10); quedan para la fase de pruebas.

## 4. Stack y estructura del repositorio

| Capa | Tecnología |
| --- | --- |
| Frontend | Angular (versión estable vigente), TypeScript estricto, componentes standalone, formularios reactivos |
| Backend | C# con ASP.NET Core (última LTS instalada), API REST, JSON |
| Persistencia | SQL Server 2022 en contenedor; EF Core para catálogo y consultas; llamadas a procedimientos almacenados para movimientos (Dapper o `ExecuteSqlRaw`) |
| Seguridad | JWT, hash de contraseñas con PBKDF2 o Argon2 mediante `PasswordHasher<T>` o equivalente con sal individual |
| Infraestructura | Docker y Docker Compose (API y SQL Server) |
| Pruebas | xUnit, Testcontainers para SQL Server, Karma/Jest en Angular |

Estilo arquitectónico: **monolito modular** con cuatro módulos internos (ITSES, 2026e).

```
inventario-ingrata/
├─ docker-compose.yml
├─ .env.example
├─ README.md
├─ docs/                      # SUPUESTOS.md, REPORTE_BETA.md
├─ db/
│  ├─ 001_schema.sql
│  ├─ 002_procedures.sql
│  ├─ 003_triggers.sql
│  └─ 004_seed.sql            # solo roles y datos ficticios
├─ backend/
│  ├─ src/Ingrata.Api/
│  │  ├─ Modules/
│  │  │  ├─ Auth/             # controladores, servicios, DTOs
│  │  │  ├─ Catalog/
│  │  │  ├─ Movements/
│  │  │  └─ Dashboard/
│  │  ├─ Shared/              # errores, bitácora, tiempo, validación
│  │  └─ Program.cs
│  └─ tests/Ingrata.Tests/
└─ frontend/                  # proyecto Angular
```

## 5. Modelo de datos (esquema físico)

Crea exactamente estas estructuras en `db/001_schema.sql`. El esquema está en 3FN; `SkuSnapshot` y `UnidadSnapshot` en `Movimiento` son copia histórica deliberada exigida por RF-18.

```sql
CREATE TABLE dbo.Rol (
  Id TINYINT PRIMARY KEY,
  Nombre NVARCHAR(30) NOT NULL UNIQUE          -- Administrador, Almacenista
);

CREATE TABLE dbo.Usuario (
  Id INT IDENTITY PRIMARY KEY,
  Identificador NVARCHAR(50) NOT NULL UNIQUE,
  NombreCompleto NVARCHAR(120) NOT NULL,
  PasswordHash NVARCHAR(400) NOT NULL,
  RolId TINYINT NOT NULL REFERENCES dbo.Rol(Id),
  Activo BIT NOT NULL DEFAULT 1,
  CreadoUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Marca (
  Id INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(80) NOT NULL UNIQUE,
  Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Categoria (
  Id INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(80) NOT NULL UNIQUE,
  Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Producto (
  Id INT IDENTITY PRIMARY KEY,
  Sku NVARCHAR(40) NOT NULL UNIQUE,
  Nombre NVARCHAR(120) NOT NULL,
  MarcaId INT NOT NULL REFERENCES dbo.Marca(Id),
  CategoriaId INT NOT NULL REFERENCES dbo.Categoria(Id),
  Unidad NVARCHAR(20) NOT NULL,                 -- botella, pieza, caja...
  Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.SaldoProducto (
  ProductoId INT PRIMARY KEY REFERENCES dbo.Producto(Id),
  Existencia INT NOT NULL DEFAULT 0 CONSTRAINT CK_Saldo_NoNegativo CHECK (Existencia >= 0)
);

CREATE SEQUENCE dbo.SeqFolio START WITH 1 INCREMENT BY 1;

CREATE TABLE dbo.Movimiento (
  Id BIGINT IDENTITY PRIMARY KEY,
  Folio NVARCHAR(20) NOT NULL UNIQUE,
  Tipo TINYINT NOT NULL CHECK (Tipo IN (1,2,3,4)),   -- 1 entrada, 2 entrega, 3 merma, 4 reversión
  ProductoId INT NOT NULL REFERENCES dbo.Producto(Id),
  SkuSnapshot NVARCHAR(40) NOT NULL,
  UnidadSnapshot NVARCHAR(20) NOT NULL,
  Cantidad INT NOT NULL CHECK (Cantidad > 0),
  UsuarioId INT NOT NULL REFERENCES dbo.Usuario(Id),
  FechaUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  Proveedor NVARCHAR(120) NULL,
  Referencia NVARCHAR(120) NULL,
  Destino NVARCHAR(120) NULL,
  Motivo NVARCHAR(300) NULL,
  MovimientoOriginalId BIGINT NULL REFERENCES dbo.Movimiento(Id),
  RequestId UNIQUEIDENTIFIER NULL,                    -- idempotencia ante reintentos
  CONSTRAINT CK_Mov_Datos CHECK (
    (Tipo = 1 AND Proveedor IS NOT NULL) OR
    (Tipo = 2 AND Destino   IS NOT NULL) OR
    (Tipo = 3 AND Motivo    IS NOT NULL) OR
    (Tipo = 4 AND Motivo    IS NOT NULL AND MovimientoOriginalId IS NOT NULL))
);
CREATE UNIQUE INDEX UX_Mov_Original ON dbo.Movimiento(MovimientoOriginalId) WHERE MovimientoOriginalId IS NOT NULL;
CREATE UNIQUE INDEX UX_Mov_RequestId ON dbo.Movimiento(RequestId) WHERE RequestId IS NOT NULL;
CREATE INDEX IX_Mov_Producto_Fecha ON dbo.Movimiento(ProductoId, FechaUtc DESC);
CREATE INDEX IX_Mov_Tipo_Fecha ON dbo.Movimiento(Tipo, FechaUtc DESC);
CREATE INDEX IX_Producto_Nombre ON dbo.Producto(Nombre);

CREATE TABLE dbo.SesionRevocada (
  Jti UNIQUEIDENTIFIER PRIMARY KEY,
  ExpiraUtc DATETIME2 NOT NULL
);

CREATE TABLE dbo.Bitacora (
  Id BIGINT IDENTITY PRIMARY KEY,
  FechaUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UsuarioId INT NULL REFERENCES dbo.Usuario(Id),
  Evento NVARCHAR(60) NOT NULL,           -- login, logout, acceso_rechazado, catalogo, usuarios, anulacion
  Detalle NVARCHAR(400) NULL
);
```

Al crear un producto, inserta también su fila en `SaldoProducto` con existencia 0 (misma transacción).

## 6. Procedimientos almacenados y trigger

Cierre atómico del movimiento y del saldo (RF-13, RF-14). Códigos de error propios: `50001` saldo insuficiente (HTTP 409), `50002` datos inválidos (400), `50003` ya anulado o no anulable (409), `50004` no encontrado (404), `50010` modificación prohibida (409).

`db/002_procedures.sql` (adapta nombres de columnas si ajustas el esquema, pero conserva la lógica):

```sql
CREATE OR ALTER PROCEDURE dbo.sp_RegistrarMovimiento
  @Tipo TINYINT, @ProductoId INT, @Cantidad INT, @UsuarioId INT,
  @Proveedor NVARCHAR(120) = NULL, @Referencia NVARCHAR(120) = NULL,
  @Destino NVARCHAR(120) = NULL, @Motivo NVARCHAR(300) = NULL,
  @RequestId UNIQUEIDENTIFIER = NULL, @Folio NVARCHAR(20) OUTPUT
AS
BEGIN
  SET NOCOUNT ON; SET XACT_ABORT ON;
  IF @Tipo NOT IN (1,2,3) OR @Cantidad <= 0 THROW 50002, 'Datos invalidos', 1;
  IF @RequestId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Movimiento WHERE RequestId = @RequestId)
  BEGIN
    SELECT @Folio = Folio FROM dbo.Movimiento WHERE RequestId = @RequestId; RETURN;
  END
  DECLARE @Sku NVARCHAR(40), @Unidad NVARCHAR(20), @Delta INT = IIF(@Tipo = 1, @Cantidad, -@Cantidad);
  BEGIN TRAN;
    SELECT @Sku = Sku, @Unidad = Unidad FROM dbo.Producto WHERE Id = @ProductoId AND Activo = 1;
    IF @Sku IS NULL THROW 50004, 'Producto inexistente o inactivo', 1;
    UPDATE dbo.SaldoProducto WITH (UPDLOCK, ROWLOCK)
       SET Existencia = Existencia + @Delta
     WHERE ProductoId = @ProductoId AND Existencia + @Delta >= 0;
    IF @@ROWCOUNT = 0 THROW 50001, 'Saldo insuficiente', 1;
    SET @Folio = CONCAT('MOV-', FORMAT(NEXT VALUE FOR dbo.SeqFolio, '000000'));
    INSERT dbo.Movimiento (Folio, Tipo, ProductoId, SkuSnapshot, UnidadSnapshot, Cantidad, UsuarioId,
                           Proveedor, Referencia, Destino, Motivo, RequestId)
    VALUES (@Folio, @Tipo, @ProductoId, @Sku, @Unidad, @Cantidad, @UsuarioId,
            @Proveedor, @Referencia, @Destino, @Motivo, @RequestId);
  COMMIT;
END
```

Crea además `dbo.sp_AnularMovimiento(@MovimientoId, @UsuarioId, @Motivo, @Folio OUTPUT)` con esta lógica, también en una sola transacción:

1. El original debe existir (`50004`), no ser tipo 4 y no tener reversión (`50003`); el motivo no puede estar vacío (`50002`).
2. Efecto de la reversión = −Δ(original): entrada se revierte restando, entrega o merma se revierten sumando.
3. Aplica el mismo `UPDATE ... WHERE Existencia + @Delta >= 0`; si afecta 0 filas, lanza `50001` (anular una entrada ya consumida).
4. Inserta el movimiento tipo 4 con el mismo producto, cantidad, SKU y unidad del original, `MovimientoOriginalId` y motivo.

`db/003_triggers.sql`: trigger `INSTEAD OF UPDATE, DELETE` sobre `dbo.Movimiento` que ejecute `THROW 50010, 'Los movimientos son inmutables', 1;` (RF-15). Aplica la misma protección a `dbo.Bitacora`.

`db/004_seed.sql`: inserta los roles (1 Administrador, 2 Almacenista) y, solo para desarrollo, algunas marcas, categorías y productos ficticios. **No insertes usuarios con contraseña por defecto.**

## 7. Reglas de negocio e invariantes

Resumen del modelo formal (ITSES, 2026d):

- `stock(p) = Σ Δ(m)` con Δ = +q en entrada, −q en entrega y merma, −Δ(original) en reversión. Siempre `stock ≥ 0`.
- Cantidades enteras positivas (unidad de control estable).
- Un movimiento confirmado no se edita ni se borra; las correcciones son contramovimientos.
- Un original se anula como máximo una vez; no se anula una reversión.
- La fecha la asigna el servidor (UTC). La interfaz muestra la hora local del establecimiento (zona configurable, por defecto `America/Mexico_City`).
- La baja lógica de usuario o producto conserva autor, SKU, unidad y cantidad históricos.
- Un producto inactivo no acepta movimientos nuevos pero sigue visible en el historial.
- Cambiar la unidad de un producto con movimientos se rechaza (requiere otro SKU).
- Dos solicitudes simultáneas por la última unidad: se confirma exactamente una.

Matriz de permisos:

| Acción | Almacenista | Administrador |
| --- | :-: | :-: |
| Consultar catálogo, existencias, historial, dashboard | Sí | Sí |
| Registrar entrada, entrega, merma | Sí | Sí |
| Gestionar catálogo (marcas, categorías, productos) | No (403) | Sí |
| Gestionar usuarios | No (403) | Sí |
| Anular movimientos | No (403) | Sí |
| Consultar bitácora | No (403) | Sí |

La autorización de mermas por Almacenista es un supuesto pendiente de confirmar con el cliente: impleméntala como configurable (`PermitirMermaAlmacenista = true`).

## 8. API REST

JSON sobre HTTPS en producción. Códigos: 400 datos inválidos, 401 no autenticado, 403 sin permiso, 404 inexistente, 409 conflicto de negocio. Errores con cuerpo `{ "codigo": "...", "mensaje": "..." }`, mensaje comprensible y sin trazas.

| Método y ruta | Rol | Descripción |
| --- | --- | --- |
| `POST /api/auth/login` | Público | Identificador y contraseña; devuelve JWT con `jti`, rol y expiración (30 min, configurable) |
| `POST /api/auth/logout` | Autenticado | Inserta el `jti` en `SesionRevocada`; el middleware rechaza tokens revocados |
| `GET/POST/PUT /api/usuarios`, `PATCH /api/usuarios/{id}/estado` | Admin | Alta, edición, activar y desactivar |
| `GET/POST/PUT /api/marcas`, `/api/categorias`; `PATCH .../{id}/estado` | Admin (GET: ambos) | Catálogos con nombre único |
| `GET /api/productos?texto=&marcaId=&categoriaId=&activo=` | Ambos | Búsqueda por SKU o nombre y filtros; incluye existencia |
| `POST/PUT /api/productos`, `PATCH /api/productos/{id}/estado` | Admin | Alta (existencia 0) y edición descriptiva |
| `POST /api/movimientos/entradas` | Ambos | Producto, cantidad, proveedor, referencia opcional, `requestId` |
| `POST /api/movimientos/entregas` | Ambos | Producto, cantidad, destino, `requestId` |
| `POST /api/movimientos/mermas` | Ambos | Producto, cantidad, motivo, `requestId` |
| `POST /api/movimientos/{id}/anulacion` | Admin | Motivo obligatorio |
| `GET /api/movimientos?desde=&hasta=&productoId=&tipo=&usuarioId=&pagina=` | Ambos | Historial filtrable; solo el Administrador puede filtrar por todos los usuarios |
| `GET /api/dashboard?desde=&hasta=` | Ambos | Existencias actuales y totales de entradas, entregas y mermas del intervalo |
| `GET /api/bitacora` | Admin | Eventos de seguridad y cambios |

Registra en `Bitacora` (RNF-06): inicio y cierre de sesión, rechazos de acceso, cambios de catálogo, gestión de usuarios y anulaciones, con actor y hora.

Alta inicial del Administrador (RD-05): al arrancar, si no existe ningún Administrador, crea uno a partir de las variables `BOOTSTRAP_ADMIN_ID` y `BOOTSTRAP_ADMIN_PASSWORD`; si faltan, la aplicación no crea cuenta alguna y registra una advertencia. Documenta el procedimiento en el README.

## 9. Diseño interno del backend

Aplica los principios SOLID y los patrones del diseño aprobado (ITSES, 2026e; Gamma et al., 1994; Fowler, 2002):

- `IMovimiento` con implementaciones `Entrada`, `Entrega`, `Merma`, `Reversion`; cada una expone el signo de su efecto sobre la existencia (**Strategy**). Un tipo nuevo no modifica `MovimientoService`.
- `MovimientoFactory` crea el movimiento concreto según el tipo solicitado (**Factory**).
- `IMovimientoRepositorio`, `ICatalogoRepositorio`, `IUsuarioRepositorio` (**Repository**) aíslan SQL Server; `IBitacoraService` registra eventos.
- `MovimientoService` solo orquesta reglas; no persiste ni audita por su cuenta.
- Dependencias por constructor mediante el contenedor de ASP.NET Core; los controladores dependen de interfaces.
- Manejo centralizado de excepciones: traduce errores SQL `50001` a `50004` y `50010` a los códigos HTTP indicados.

## 10. Frontend (Angular)

Diseño móvil primero, utilizable desde 360 px y en tableta sin desplazamiento horizontal en el contenido principal (IU-01).

Pantallas mínimas:

1. **Login** y cierre de sesión; intercepta 401 y redirige al login.
2. **Layout** con navegación según rol (oculta lo no permitido; la API sigue siendo la autoridad).
3. **Productos:** lista con búsqueda y filtros, estado vacío explícito, formulario de alta y edición (Admin).
4. **Marcas y categorías** (Admin) y **Usuarios** (Admin).
5. **Nuevo movimiento:** selector de tipo (entrada, entrega, merma), campos condicionales (proveedor, destino o motivo), confirmación con folio. Genera un `requestId` (UUID) por intento para evitar duplicados.
6. **Historial:** filtros por fecha, producto y tipo; distingue visualmente entradas, salidas, mermas y anuladas (IU-03); acción de anular con motivo (Admin).
7. **Dashboard:** existencias actuales y totales del periodo seleccionado.

Requisitos de interfaz: unidades visibles, campos obligatorios marcados, errores junto al campo, datos capturados conservados ante error (IU-02), mensajes claros ante pérdida de conexión (RNF-12), horas mostradas en zona local con `Intl.DateTimeFormat`.

## 11. Plan de fases y criterios de aceptación

Fechas de referencia del cronograma (ITSES, 2026c): acceso y catálogos 21/10 a 03/11; movimientos 04/11 a 17/11; dashboard e integración 18/11 a 27/11.

| Fase | Contenido | Criterio de aceptación |
| --- | --- | --- |
| 0. Base | Repo, Docker Compose, SQL Server arriba, API vacía con `/health`, Angular generado, `.env.example` | `docker compose up` levanta API y BD; `/health` responde 200 |
| 1. Base de datos | Scripts 001 a 004 aplicados automáticamente al iniciar | Tablas, índices, SP y triggers existen; `UPDATE` o `DELETE` sobre `Movimiento` falla |
| 2. Acceso y usuarios | HU-01 a HU-04 | Login válido e inválido, logout invalida el token, Almacenista recibe 403, cuenta desactivada pierde acceso |
| 3. Catálogo | HU-05 a HU-09 | SKU y nombres duplicados rechazados; cambio de unidad con historial rechazado; baja lógica; filtros y vacío |
| 4. Movimientos | HU-10 a HU-18 | Ejemplos del ERS: 2 + 5 = 7, entrega de 2 deja 5, merma de 1 deja 4; saldo nunca negativo; folio único; anulación una sola vez |
| 5. Consultas | HU-19 a HU-21 | Dashboard y historial coinciden con el conjunto de prueba; misma hora local en dos consultas |
| 6. Pruebas y cierre | Pruebas automáticas, README, reporte | Todas las pruebas de la sección 12 en verde |

## 12. Pruebas obligatorias

Implementa pruebas de integración con SQL Server real (Testcontainers), derivadas de la RTM (ITSES, 2026b):

| Prueba | Qué verifica |
| --- | --- |
| PT-01, PT-02 | Login con credenciales activas e inválidas; token inutilizable tras logout |
| PT-03 | Almacenista llama directamente rutas de catálogo, usuarios y anulación: 403 y sin cambios |
| PT-06, PT-07 | SKU único y obligatorios; editar nombre conserva folios; cambiar unidad con historial se rechaza |
| PT-10 a PT-12 | Entrada, entrega y merma válidas e inválidas (cantidad 0, sin proveedor, destino o motivo) |
| **PT-13** | Con saldo 1, dos entregas simultáneas de 1 (ejecución en paralelo): exactamente una confirmada y saldo final 0 |
| **PT-14** | Falla provocada tras el `UPDATE` de saldo: ni movimiento ni saldo cambian (rollback) |
| PT-15 | Intento de `UPDATE` y `DELETE` directo sobre `Movimiento` es rechazado |
| PT-16 | Anular una entrega repone saldo; segunda anulación rechazada; anular entrada consumida rechazada |
| PT-17 | Para cada producto, `SaldoProducto.Existencia` = suma algebraica del historial |
| PT-18 | Desactivar usuario y producto conserva autor, SKU y unidad en el historial |
| PT-21 | Mismo folio consultado dos veces muestra la misma hora local |
| PT-28, PT-38 | Cargas inválidas directas a la API devuelven 400, 401, 403, 404 o 409 según el caso |

También: un reintento con el mismo `requestId` no crea un segundo movimiento.

## 13. Docker y configuración

`docker-compose.yml` con dos servicios (`db` con la imagen oficial de SQL Server 2022 y `api`), volumen persistente para datos, `depends_on` con espera a que la BD acepte conexiones y ejecución de los scripts de `db/` al iniciar. Variables en `.env.example`:

```
ACCEPT_EULA=Y
MSSQL_SA_PASSWORD=<definir>
ConnectionStrings__Default=<definir>
Jwt__Key=<definir, mínimo 32 caracteres>
Jwt__ExpireMinutes=30
App__TimeZone=America/Mexico_City
BOOTSTRAP_ADMIN_ID=<definir>
BOOTSTRAP_ADMIN_PASSWORD=<definir>
```

El frontend, en la beta, se sirve con `ng serve` o un contenedor Nginx opcional; el alojamiento final queda pendiente de acordar con el cliente (ITSES, 2026a). Habilita CORS solo para el origen del frontend.

## 14. Definición de terminado (beta)

- [ ] `docker compose up` y los pasos del README levantan el sistema desde cero en otro equipo.
- [ ] Las 21 HU tienen su escenario BDD cubierto por una prueba o verificación manual documentada.
- [ ] PT-13 y PT-14 pasan de forma repetible.
- [ ] Ningún secreto en el repositorio; no existe usuario con contraseña por defecto.
- [ ] La interfaz funciona a 360 px y en tableta.
- [ ] `docs/SUPUESTOS.md` y `docs/REPORTE_BETA.md` están completos.

## 15. Supuestos y decisiones pendientes con La Ingrata

No bloquean la beta; impleméntalos con el valor por defecto indicado y regístralos en `docs/SUPUESTOS.md`:

| Decisión pendiente | Valor por defecto en la beta |
| --- | --- |
| Unidades fraccionarias (litros, kg) | No; cantidades enteras |
| Inventario por barra | No; la barra es un texto de destino |
| Quién autoriza mermas | Ambos roles, configurable |
| Expiración de sesión | 30 min de inactividad (propuesta) |
| Inventario inicial | Se carga como entrada con referencia «saldo inicial» |
| Zona horaria | `America/Mexico_City`, configurable |
| Hosting del frontend y certificados | Sin definir; desarrollo con `ng serve` |

---

## Referencias

Fowler, M. (2002). *Patterns of enterprise application architecture*. Addison-Wesley.

Gamma, E., Helm, R., Johnson, R., & Vlissides, J. (1994). *Design patterns: Elements of reusable object-oriented software*. Addison-Wesley.

Instituto Tecnológico Sanmiguelense de Estudios Superiores [ITSES], equipo del proyecto. (2026a). *Especificación de requisitos de software: Plataforma web de control de inventarios — La Ingrata* (Versión 0.1) [Documento de curso no publicado].

Instituto Tecnológico Sanmiguelense de Estudios Superiores, equipo del proyecto. (2026b). *Historias de usuario y escenarios BDD; Matriz de trazabilidad de requisitos (RTM)* [Documento de curso no publicado].

Instituto Tecnológico Sanmiguelense de Estudios Superiores, equipo del proyecto. (2026c). *Memoria de estimación y planificación* [Documento de curso no publicado].

Instituto Tecnológico Sanmiguelense de Estudios Superiores, equipo del proyecto. (2026d). *Modelado formal y estructural del sistema: Plataforma web de control de inventarios — La Ingrata* (Versión 1.0) [Documento de curso no publicado].

Instituto Tecnológico Sanmiguelense de Estudios Superiores, equipo del proyecto. (2026e). *Taller de diagramas y algoritmos: Arquitectura y diseño de clases* [Documento de curso no publicado].
