using System.ComponentModel.DataAnnotations;

namespace Ingrata.Api.Modules.Movements.Dtos;

public class SolicitudMovimiento
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser un entero positivo.")]
    public int Cantidad { get; set; }

    [MaxLength(120, ErrorMessage = "El proveedor no puede exceder 120 caracteres.")]
    public string? Proveedor { get; set; }

    [MaxLength(120, ErrorMessage = "La referencia no puede exceder 120 caracteres.")]
    public string? Referencia { get; set; }

    [MaxLength(120, ErrorMessage = "El destino no puede exceder 120 caracteres.")]
    public string? Destino { get; set; }

    [MaxLength(300, ErrorMessage = "El motivo no puede exceder 300 caracteres.")]
    public string? Motivo { get; set; }

    /// <summary>Identificador de idempotencia: un reintento con el mismo valor no duplica el movimiento.</summary>
    public Guid? RequestId { get; set; }
}

public class AnulacionRequest
{
    [Required(ErrorMessage = "El motivo de anulacion es obligatorio.")]
    [MaxLength(300, ErrorMessage = "El motivo no puede exceder 300 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class ResultadoMovimiento
{
    public long? Id { get; set; }
    public string Folio { get; set; } = "";
    public byte Tipo { get; set; }
    public string TipoNombre { get; set; } = "";
    public int Efecto { get; set; }
    public int? ExistenciaResultante { get; set; }
    public string Mensaje { get; set; } = "";
}

public class FiltroMovimientos
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProductoId { get; set; }
    public byte? Tipo { get; set; }
    public int? UsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
}

public class MovimientoDto
{
    public long Id { get; set; }
    public string Folio { get; set; } = "";
    public byte Tipo { get; set; }
    public string TipoNombre { get; set; } = "";
    public int ProductoId { get; set; }
    public string Sku { get; set; } = "";
    public string ProductoNombre { get; set; } = "";
    public string Unidad { get; set; } = "";
    public int Cantidad { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = "";
    public DateTime FechaUtc { get; set; }
    public string? Proveedor { get; set; }
    public string? Referencia { get; set; }
    public string? Destino { get; set; }
    public string? Motivo { get; set; }
    public long? MovimientoOriginalId { get; set; }
    public string? FolioOriginal { get; set; }
    public bool Anulado { get; set; }
    public bool EsReversion => Tipo == 4;
}
