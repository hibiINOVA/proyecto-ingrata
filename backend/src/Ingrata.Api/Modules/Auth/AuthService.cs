using System.Security.Claims;
using Ingrata.Api.Modules.Auth.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Errores;
using Ingrata.Api.Shared.Validacion;

namespace Ingrata.Api.Modules.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest solicitud, HttpContext contexto);
    Task LogoutAsync(HttpContext contexto);
}

public sealed class AuthService : IAuthService
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IJwtTokenService _tokens;
    private readonly IBitacoraService _bitacora;

    public AuthService(IUsuarioRepositorio usuarios, IJwtTokenService tokens, IBitacoraService bitacora)
    {
        _usuarios = usuarios;
        _tokens = tokens;
        _bitacora = bitacora;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest solicitud, HttpContext contexto)
    {
        var identificador = solicitud.Identificador.Trim();
        var usuario = await _usuarios.PorIdentificadorAsync(identificador);

        if (usuario is null || !PasswordHasher.Verify(solicitud.Password, usuario.PasswordHash))
        {
            await _bitacora.RegistrarAsync("acceso_rechazado", usuario?.Id,
                $"Login fallido para el identificador '{identificador}'.");
            throw new ExcepcionDeNegocio(CodigosError.NoAutenticado,
                "Identificador o contrasena incorrectos.", 401);
        }

        if (!usuario.Activo)
        {
            await _bitacora.RegistrarAsync("acceso_rechazado", usuario.Id,
                $"Intento de login con cuenta desactivada '{identificador}'.");
            throw new ExcepcionDeNegocio(CodigosError.NoAutenticado,
                "La cuenta esta desactivada. Contacte al administrador.", 401);
        }

        var (token, expiraUtc, _) = _tokens.Crear(usuario);

        await _bitacora.RegistrarAsync("login", usuario.Id,
            $"Sesion iniciada por '{identificador}'.");

        return new LoginResponse
        {
            Token = token,
            ExpiraUtc = expiraUtc,
            Usuario = new UsuarioSesionDto
            {
                Id = usuario.Id,
                Identificador = usuario.Identificador,
                NombreCompleto = usuario.NombreCompleto,
                RolId = usuario.RolId,
                Rol = usuario.Rol?.Nombre ?? ""
            }
        };
    }

    public async Task LogoutAsync(HttpContext contexto)
    {
        var usuarioId = (int?)null;
        var jtiTexto = contexto.User?.FindFirstValue("jti");
        var expiracion = contexto.User?.FindFirstValue("exp");

        if (int.TryParse(contexto.User?.FindFirstValue("usuario_id"), out var id))
            usuarioId = id;

        if (Guid.TryParse(jtiTexto, out var jti))
        {
            var expiraUtc = DateTime.UtcNow.AddMinutes(5);
            if (long.TryParse(expiracion, out var expUnix))
                expiraUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            await _usuarios.RevocarSesionAsync(jti, expiraUtc);
        }

        await _bitacora.RegistrarAsync("logout", usuarioId, "Sesion cerrada.");
    }
}
