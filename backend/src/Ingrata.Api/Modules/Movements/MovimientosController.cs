using Ingrata.Api.Modules.Movements.Dtos;
using Ingrata.Api.Shared.Claims;
using Ingrata.Api.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ingrata.Api.Modules.Movements;

[ApiController]
[Route("api/movimientos")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly IMovimientoService _servicio;

    public MovimientosController(IMovimientoService servicio) => _servicio = servicio;

    [HttpPost("entradas")]
    public Task<ActionResult<ResultadoMovimiento>> RegistrarEntrada([FromBody] SolicitudMovimiento solicitud)
        => Registrar(1, solicitud);

    [HttpPost("entregas")]
    public Task<ActionResult<ResultadoMovimiento>> RegistrarEntrega([FromBody] SolicitudMovimiento solicitud)
        => Registrar(2, solicitud);

    [HttpPost("mermas")]
    public Task<ActionResult<ResultadoMovimiento>> RegistrarMerma([FromBody] SolicitudMovimiento solicitud)
        => Registrar(3, solicitud);

    [HttpPost("{id:long}/anulacion")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ResultadoMovimiento>> Anular(long id, [FromBody] AnulacionRequest solicitud)
    {
        var resultado = await _servicio.AnularAsync(id, solicitud.Motivo,
            HttpContext.User.ObtenerUsuarioId());
        return Ok(resultado);
    }

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<MovimientoDto>>> Historial([FromQuery] FiltroMovimientos filtros)
    {
        var usuarioId = HttpContext.User.ObtenerUsuarioId();
        var esAdmin = HttpContext.User.ObtenerRol() == "Administrador";
        var resultado = await _servicio.HistorialAsync(filtros, usuarioId, esAdmin);
        return Ok(resultado);
    }

    private async Task<ActionResult<ResultadoMovimiento>> Registrar(byte tipo, SolicitudMovimiento solicitud)
    {
        var resultado = await _servicio.RegistrarAsync(tipo, solicitud,
            HttpContext.User.ObtenerUsuarioId(), HttpContext.User.ObtenerRol());
        return Ok(resultado);
    }
}
