using System.ComponentModel.DataAnnotations;

namespace Ingrata.Api.Shared.Dtos;

public class CambiarEstadoRequest
{
    [Required(ErrorMessage = "El campo activo es obligatorio.")]
    public bool Activo { get; set; }
}

public class RespuestaPaginada<T>
{
    public int Pagina { get; set; }
    public int Tamano { get; set; }
    public long Total { get; set; }
    public List<T> Elementos { get; set; } = new();
}
