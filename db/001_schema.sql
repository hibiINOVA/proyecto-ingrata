/* 001_schema.sql — Esquema físico de Inventario Ingrata (3FN)
   Idempotente: puede ejecutarse mas de una vez sin error. */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Rol', 'U') IS NULL
CREATE TABLE dbo.Rol (
  Id TINYINT PRIMARY KEY,
  Nombre NVARCHAR(30) NOT NULL UNIQUE
);
GO

IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
CREATE TABLE dbo.Usuario (
  Id INT IDENTITY PRIMARY KEY,
  Identificador NVARCHAR(50) NOT NULL UNIQUE,
  NombreCompleto NVARCHAR(120) NOT NULL,
  PasswordHash NVARCHAR(400) NOT NULL,
  RolId TINYINT NOT NULL REFERENCES dbo.Rol(Id),
  Activo BIT NOT NULL DEFAULT 1,
  CreadoUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID('dbo.Marca', 'U') IS NULL
CREATE TABLE dbo.Marca (
  Id INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(80) NOT NULL UNIQUE,
  Activo BIT NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dbo.Categoria', 'U') IS NULL
CREATE TABLE dbo.Categoria (
  Id INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(80) NOT NULL UNIQUE,
  Activo BIT NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dbo.Producto', 'U') IS NULL
CREATE TABLE dbo.Producto (
  Id INT IDENTITY PRIMARY KEY,
  Sku NVARCHAR(40) NOT NULL UNIQUE,
  Nombre NVARCHAR(120) NOT NULL,
  MarcaId INT NOT NULL REFERENCES dbo.Marca(Id),
  CategoriaId INT NOT NULL REFERENCES dbo.Categoria(Id),
  Unidad NVARCHAR(20) NOT NULL,
  Activo BIT NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dbo.SaldoProducto', 'U') IS NULL
CREATE TABLE dbo.SaldoProducto (
  ProductoId INT PRIMARY KEY REFERENCES dbo.Producto(Id),
  Existencia INT NOT NULL DEFAULT 0 CONSTRAINT CK_Saldo_NoNegativo CHECK (Existencia >= 0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'SeqFolio')
CREATE SEQUENCE dbo.SeqFolio START WITH 1 INCREMENT BY 1;
GO

IF OBJECT_ID('dbo.Movimiento', 'U') IS NULL
CREATE TABLE dbo.Movimiento (
  Id BIGINT IDENTITY PRIMARY KEY,
  Folio NVARCHAR(20) NOT NULL UNIQUE,
  Tipo TINYINT NOT NULL CHECK (Tipo IN (1,2,3,4)),
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
  RequestId UNIQUEIDENTIFIER NULL,
  CONSTRAINT CK_Mov_Datos CHECK (
    (Tipo = 1 AND Proveedor IS NOT NULL) OR
    (Tipo = 2 AND Destino   IS NOT NULL) OR
    (Tipo = 3 AND Motivo    IS NOT NULL) OR
    (Tipo = 4 AND Motivo    IS NOT NULL AND MovimientoOriginalId IS NOT NULL))
);
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Mov_Original' AND object_id = OBJECT_ID('dbo.Movimiento'))
CREATE UNIQUE INDEX UX_Mov_Original ON dbo.Movimiento(MovimientoOriginalId) WHERE MovimientoOriginalId IS NOT NULL;
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Mov_RequestId' AND object_id = OBJECT_ID('dbo.Movimiento'))
CREATE UNIQUE INDEX UX_Mov_RequestId ON dbo.Movimiento(RequestId) WHERE RequestId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mov_Producto_Fecha' AND object_id = OBJECT_ID('dbo.Movimiento'))
CREATE INDEX IX_Mov_Producto_Fecha ON dbo.Movimiento(ProductoId, FechaUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mov_Tipo_Fecha' AND object_id = OBJECT_ID('dbo.Movimiento'))
CREATE INDEX IX_Mov_Tipo_Fecha ON dbo.Movimiento(Tipo, FechaUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Producto_Nombre' AND object_id = OBJECT_ID('dbo.Producto'))
CREATE INDEX IX_Producto_Nombre ON dbo.Producto(Nombre);
GO

IF OBJECT_ID('dbo.SesionRevocada', 'U') IS NULL
CREATE TABLE dbo.SesionRevocada (
  Jti UNIQUEIDENTIFIER PRIMARY KEY,
  ExpiraUtc DATETIME2 NOT NULL
);
GO

IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL
CREATE TABLE dbo.Bitacora (
  Id BIGINT IDENTITY PRIMARY KEY,
  FechaUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UsuarioId INT NULL REFERENCES dbo.Usuario(Id),
  Evento NVARCHAR(60) NOT NULL,
  Detalle NVARCHAR(400) NULL
);
GO
