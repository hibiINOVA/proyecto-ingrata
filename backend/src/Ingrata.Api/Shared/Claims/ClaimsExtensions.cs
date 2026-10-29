using System.Security.Claims;
using Ingrata.Api.Shared.Errores;

namespace Ingrata.Api.Shared.Claims;

public static class ClaimsExtensions
{
    public const string ClaimUsuarioId = "usuario_id";
    public const string ClaimJti = "jti";

    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimUsuarioId);
        if (valor is null || !int.TryParse(valor, out var id))
            throw new ExcepcionDeNegocio(CodigosError.NoAutenticado,
                "La sesion no contiene un usuario valido.", 401);
        return id;
    }

    public static string? ObtenerNombreUsuario(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue("nombre") ?? usuario.FindFirstValue(ClaimTypes.Name);

    public static string? ObtenerIdentificador(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue("identificador");

    public static string ObtenerRol(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue("rol") ?? usuario.FindFirstValue(ClaimTypes.Role) ?? "";
}
