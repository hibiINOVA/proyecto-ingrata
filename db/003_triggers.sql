/* 003_triggers.sql — Inmutabilidad de Movimiento y Bitacora (RF-15) */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Mov_Inmutable')
  DROP TRIGGER dbo.TR_Mov_Inmutable;
GO

CREATE TRIGGER dbo.TR_Mov_Inmutable
ON dbo.Movimiento
INSTEAD OF UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  THROW 50010, 'Los movimientos son inmutables', 1;
END
GO

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Bitacora_Inmutable')
  DROP TRIGGER dbo.TR_Bitacora_Inmutable;
GO

CREATE TRIGGER dbo.TR_Bitacora_Inmutable
ON dbo.Bitacora
INSTEAD OF UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  THROW 50010, 'La bitacora es inmutable', 1;
END
GO
