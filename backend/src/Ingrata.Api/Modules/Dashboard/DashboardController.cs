using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Dashboard;

[ApiController]
[Route("api")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Obtener([FromQuery] FiltroDashboard filtro)
        => Ok(await _dashboard.ObtenerAsync(filtro));
}

public class BitacoraDto
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public int? UsuarioId { get; set; }
    public string? Usuario { get; set; }
    public string Evento { get; set; } = "";
    public string? Detalle { get; set; }
}

[ApiController]
[Route("api/bitacora")]
[Authorize(Roles = "Administrador")]
public class BitacoraController : ControllerBase
{
    private readonly IngrataDbContext _db;

    public BitacoraController(IngrataDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<BitacoraDto>>> Listar([FromQuery] int pagina = 1)
    {
        pagina = Math.Max(1, pagina);
        const int tamano = 50;

        var consulta = _db.Bitacora.AsNoTracking();
        var total = await consulta.CountAsync();

        var elementos = await consulta
            .OrderByDescending(b => b.FechaUtc)
            .ThenByDescending(b => b.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(b => new BitacoraDto
            {
                Id = b.Id,
                FechaUtc = b.FechaUtc,
                UsuarioId = b.UsuarioId,
                Usuario = b.Usuario != null ? b.Usuario.NombreCompleto : null,
                Evento = b.Evento,
                Detalle = b.Detalle
            })
            .ToListAsync();

        foreach (var item in elementos)
            item.FechaUtc = DateTime.SpecifyKind(item.FechaUtc, DateTimeKind.Utc);

        return Ok(new RespuestaPaginada<BitacoraDto>
        {
            Pagina = pagina,
            Tamano = tamano,
            Total = total,
            Elementos = elementos
        });
    }
}
