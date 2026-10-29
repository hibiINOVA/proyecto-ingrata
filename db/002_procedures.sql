/* 002_procedures.sql — Cierre atomico de movimientos y saldo (RF-13, RF-14)
   Codigos de error: 50001 saldo insuficiente, 50002 datos invalidos,
   50003 ya anulado o no anulable, 50004 no encontrado. */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarMovimiento
  @Tipo TINYINT,
  @ProductoId INT,
  @Cantidad INT,
  @UsuarioId INT,
  @Proveedor NVARCHAR(120) = NULL,
  @Referencia NVARCHAR(120) = NULL,
  @Destino NVARCHAR(120) = NULL,
  @Motivo NVARCHAR(300) = NULL,
  @RequestId UNIQUEIDENTIFIER = NULL,
  @Folio NVARCHAR(20) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  IF @Tipo NOT IN (1, 2, 3) OR @Cantidad IS NULL OR @Cantidad <= 0
    THROW 50002, 'Datos invalidos', 1;

  IF @RequestId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Movimiento WHERE RequestId = @RequestId)
  BEGIN
    SELECT @Folio = Folio FROM dbo.Movimiento WHERE RequestId = @RequestId;
    RETURN;
  END

  DECLARE @Sku NVARCHAR(40), @Unidad NVARCHAR(20);
  DECLARE @Delta INT = IIF(@Tipo = 1, @Cantidad, -@Cantidad);

  BEGIN TRAN;

    SELECT @Sku = Sku, @Unidad = Unidad
      FROM dbo.Producto
     WHERE Id = @ProductoId AND Activo = 1;

    IF @Sku IS NULL
    BEGIN
      ROLLBACK;
      THROW 50004, 'Producto inexistente o inactivo', 1;
    END

    UPDATE dbo.SaldoProducto WITH (UPDLOCK, ROWLOCK)
       SET Existencia = Existencia + @Delta
     WHERE ProductoId = @ProductoId AND Existencia + @Delta >= 0;

    IF @@ROWCOUNT = 0
    BEGIN
      ROLLBACK;
      THROW 50001, 'Saldo insuficiente', 1;
    END

    SET @Folio = CONCAT('MOV-', FORMAT(NEXT VALUE FOR dbo.SeqFolio, '000000'));

    INSERT dbo.Movimiento (Folio, Tipo, ProductoId, SkuSnapshot, UnidadSnapshot, Cantidad, UsuarioId,
                           Proveedor, Referencia, Destino, Motivo, RequestId)
    VALUES (@Folio, @Tipo, @ProductoId, @Sku, @Unidad, @Cantidad, @UsuarioId,
            @Proveedor, @Referencia, @Destino, @Motivo, @RequestId);

  COMMIT;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AnularMovimiento
  @MovimientoId BIGINT,
  @UsuarioId INT,
  @Motivo NVARCHAR(300),
  @Folio NVARCHAR(20) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  IF @Motivo IS NULL OR LTRIM(RTRIM(@Motivo)) = ''
    THROW 50002, 'El motivo de anulacion es obligatorio', 1;

  DECLARE @TipoOriginal TINYINT, @ProductoId INT, @Cantidad INT;
  DECLARE @Sku NVARCHAR(40), @Unidad NVARCHAR(20), @OriginalRevertido BIT = 0;
  DECLARE @Delta INT;

  BEGIN TRAN;

    SELECT @TipoOriginal = Tipo, @ProductoId = ProductoId, @Cantidad = Cantidad,
           @Sku = SkuSnapshot, @Unidad = UnidadSnapshot
      FROM dbo.Movimiento WITH (UPDLOCK, ROWLOCK)
     WHERE Id = @MovimientoId;

    IF @TipoOriginal IS NULL
    BEGIN
      ROLLBACK;
      THROW 50004, 'Movimiento inexistente', 1;
    END

    IF @TipoOriginal = 4
    BEGIN
      ROLLBACK;
      THROW 50003, 'No se puede anular una anulacion', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.Movimiento WHERE MovimientoOriginalId = @MovimientoId)
    BEGIN
      ROLLBACK;
      THROW 50003, 'El movimiento ya fue anulado', 1;
    END

    -- Efecto de la reversión = −Δ(original): entrada resta, entrega/merma suman
    SET @Delta = IIF(@TipoOriginal = 1, -@Cantidad, @Cantidad);

    UPDATE dbo.SaldoProducto WITH (UPDLOCK, ROWLOCK)
       SET Existencia = Existencia + @Delta
     WHERE ProductoId = @ProductoId AND Existencia + @Delta >= 0;

    IF @@ROWCOUNT = 0
    BEGIN
      ROLLBACK;
      THROW 50001, 'Saldo insuficiente para anular: el movimiento original ya fue consumido', 1;
    END

    SET @Folio = CONCAT('MOV-', FORMAT(NEXT VALUE FOR dbo.SeqFolio, '000000'));

    INSERT dbo.Movimiento (Folio, Tipo, ProductoId, SkuSnapshot, UnidadSnapshot, Cantidad, UsuarioId,
                           Motivo, MovimientoOriginalId)
    VALUES (@Folio, 4, @ProductoId, @Sku, @Unidad, @Cantidad, @UsuarioId, @Motivo, @MovimientoId);

  COMMIT;
END
GO
