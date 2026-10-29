namespace Ingrata.Api.Shared.Datos;

public class RolEntity
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class UsuarioEntity
{
    public int Id { get; set; }
    public string Identificador { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public byte RolId { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoUtc { get; set; }

    public RolEntity? Rol { get; set; }
}

public class MarcaEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

public class CategoriaEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

public class ProductoEntity
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int MarcaId { get; set; }
    public int CategoriaId { get; set; }
    public string Unidad { get; set; } = "";
    public bool Activo { get; set; }

    public MarcaEntity? Marca { get; set; }
    public CategoriaEntity? Categoria { get; set; }
    public SaldoProductoEntity? Saldo { get; set; }
}

public class SaldoProductoEntity
{
    public int ProductoId { get; set; }
    public int Existencia { get; set; }

    public ProductoEntity? Producto { get; set; }
}

public class MovimientoEntity
{
    public long Id { get; set; }
    public string Folio { get; set; } = "";
    public byte Tipo { get; set; }
    public int ProductoId { get; set; }
    public string SkuSnapshot { get; set; } = "";
    public string UnidadSnapshot { get; set; } = "";
    public int Cantidad { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaUtc { get; set; }
    public string? Proveedor { get; set; }
    public string? Referencia { get; set; }
    public string? Destino { get; set; }
    public string? Motivo { get; set; }
    public long? MovimientoOriginalId { get; set; }
    public Guid? RequestId { get; set; }

    public ProductoEntity? Producto { get; set; }
    public UsuarioEntity? Usuario { get; set; }
    public MovimientoEntity? MovimientoOriginal { get; set; }
}

public class SesionRevocadaEntity
{
    public Guid Jti { get; set; }
    public DateTime ExpiraUtc { get; set; }
}

public class BitacoraEntity
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public int? UsuarioId { get; set; }
    public string Evento { get; set; } = "";
    public string? Detalle { get; set; }

    public UsuarioEntity? Usuario { get; set; }
}
