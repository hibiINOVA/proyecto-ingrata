using System.ComponentModel.DataAnnotations;

namespace Ingrata.Api.Modules.Auth.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "El identificador es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El identificador no puede exceder 50 caracteres.")]
    public string Identificador { get; set; } = "";

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    public string Password { get; set; } = "";
}

public class UsuarioSesionDto
{
    public int Id { get; set; }
    public string Identificador { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public byte RolId { get; set; }
    public string Rol { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public UsuarioSesionDto Usuario { get; set; } = new();
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Identificador { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public byte RolId { get; set; }
    public string Rol { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime CreadoUtc { get; set; }
}

public class CrearUsuarioRequest
{
    [Required(ErrorMessage = "El identificador es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El identificador no puede exceder 50 caracteres.")]
    public string Identificador { get; set; } = "";

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre completo no puede exceder 120 caracteres.")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public byte RolId { get; set; }
}

public class EditarUsuarioRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre completo no puede exceder 120 caracteres.")]
    public string NombreCompleto { get; set; } = "";

    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public byte RolId { get; set; }
}
