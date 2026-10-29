using Ingrata.Api.Modules.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ingrata.Api.Modules.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest solicitud)
    {
        var respuesta = await _auth.LoginAsync(solicitud, HttpContext);
        return Ok(respuesta);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync(HttpContext);
        return Ok(new { mensaje = "Sesion cerrada correctamente." });
    }
}
