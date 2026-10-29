using System.ComponentModel.DataAnnotations;

namespace Ingrata.Api.Modules.Catalog.Dtos;

public class MarcaRequest
{
    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [MaxLength(80, ErrorMessage = "El nombre no puede exceder 80 caracteres.")]
    public string Nombre { get; set; } = "";
}

public class MarcaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

public class CategoriaRequest
{
    [Required(ErrorMessage = "El nombre de la categoria es obligatorio.")]
    [MaxLength(80, ErrorMessage = "El nombre no puede exceder 80 caracteres.")]
    public string Nombre { get; set; } = "";
}

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

public class ProductoRequest
{
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [MaxLength(40, ErrorMessage = "El SKU no puede exceder 40 caracteres.")]
    public string Sku { get; set; } = "";

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre no puede exceder 120 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "La marca es obligatoria.")]
    public int MarcaId { get; set; }

    [Required(ErrorMessage = "La categoria es obligatoria.")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [MaxLength(20, ErrorMessage = "La unidad no puede exceder 20 caracteres.")]
    public string Unidad { get; set; } = "";
}

public class ProductoDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int MarcaId { get; set; }
    public string Marca { get; set; } = "";
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = "";
    public string Unidad { get; set; } = "";
    public bool Activo { get; set; }
    public int Existencia { get; set; }
}

public class ProductoFiltros
{
    public string? Texto { get; set; }
    public int? MarcaId { get; set; }
    public int? CategoriaId { get; set; }
    public bool? Activo { get; set; }
}
