/* 004_seed.sql — Roles (obligatorios) y datos ficticios de desarrollo.
   NO se insertan usuarios con contraseña por defecto (RD-05 / definición de terminado). */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Id = 1)
  INSERT dbo.Rol (Id, Nombre) VALUES (1, 'Administrador');
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Id = 2)
  INSERT dbo.Rol (Id, Nombre) VALUES (2, 'Almacenista');
GO

/* ---- Datos ficticios solo para desarrollo ---- */
IF NOT EXISTS (SELECT 1 FROM dbo.Marca)
BEGIN
  INSERT dbo.Marca (Nombre) VALUES
    ('Corona'), ('Modelo'), ('Jose Cuervo'), ('Bacardi'), ('Don Julio');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categoria)
BEGIN
  INSERT dbo.Categoria (Nombre) VALUES
    ('Cerveza'), ('Destilado'), ('Refresco'), ('Vinagre'), ('Desechables');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Producto)
BEGIN
  INSERT dbo.Producto (Sku, Nombre, MarcaId, CategoriaId, Unidad)
  VALUES
    ('CER-COR-355', 'Corona Extra 355 ml', 1, 1, 'botella'),
    ('CER-MOD-355', 'Modelo Especial 355 ml', 2, 1, 'botella'),
    ('DES-JC-750',  'Jose Cuervo Especial 750 ml', 3, 2, 'botella'),
    ('DES-BAC-750', 'Bacardi Blanco 750 ml', 4, 2, 'botella'),
    ('DES-DJ-750',  'Don Julio Blanco 750 ml', 5, 2, 'botella'),
    ('REF-COCA-600','Coca-Cola 600 ml', 1, 3, 'botella'),
    ('DES-VIN-1L',  'Vinagre blanco 1 L', 2, 4, 'botella'),
    ('DES-COP-50',  'Vaso plastico 500 ml', 3, 5, 'caja');

  INSERT dbo.SaldoProducto (ProductoId, Existencia)
  SELECT Id, 0 FROM dbo.Producto;
END
GO
