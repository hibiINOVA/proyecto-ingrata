using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Opciones;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ingrata.Api.Modules.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiraUtc, Guid Jti) Crear(UsuarioEntity usuario);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opciones;

    public JwtTokenService(IOptions<JwtOptions> opciones) => _opciones = opciones.Value;

    public (string Token, DateTime ExpiraUtc, Guid Jti) Crear(UsuarioEntity usuario)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Key) || _opciones.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key no esta definido o tiene menos de 32 caracteres. Configuralo en variables de entorno.");

        var jti = Guid.NewGuid();
        var expira = DateTime.UtcNow.AddMinutes(_opciones.ExpireMinutes);

        var claims = new List<Claim>
        {
            new("jti", jti.ToString()),
            new("usuario_id", usuario.Id.ToString()),
            new("identificador", usuario.Identificador),
            new("nombre", usuario.NombreCompleto),
            new("rol", usuario.Rol?.Nombre ?? ""),
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Identificador)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Key));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Issuer,
            audience: _opciones.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expira,
            signingCredentials: credenciales);

        var texto = new JwtSecurityTokenHandler().WriteToken(token);
        return (texto, expira, jti);
    }
}
