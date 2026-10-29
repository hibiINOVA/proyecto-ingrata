using System.Security.Claims;
using Ingrata.Api.Shared.Datos;

namespace Ingrata.Api.Shared.Bitacora;

public interface IBitacoraService
{
    Task RegistrarAsync(string evento, int? usuarioId, string? detalle = null);
    Task RegistrarDesdeContextoAsync(HttpContext contexto, string evento, string? detalle = null);
}

public sealed class BitacoraService : IBitacoraService
{
    private readonly IngrataDbContext _db;
    private readonly ILogger<BitacoraService> _logger;

    public BitacoraService(IngrataDbContext db, ILogger<BitacoraService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegistrarAsync(string evento, int? usuarioId, string? detalle = null)
    {
        try
        {
            _db.Bitacora.Add(new BitacoraEntity
            {
                FechaUtc = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Evento = evento,
                Detalle = detalle is { Length: > 400 } ? detalle[..400] : detalle
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar el evento de bitacora {Evento}", evento);
        }
    }

    public Task RegistrarDesdeContextoAsync(HttpContext contexto, string evento, string? detalle = null)
    {
        int? usuarioId = null;
        var claim = contexto.User?.FindFirstValue("usuario_id") ?? contexto.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var id))
            usuarioId = id;
        return RegistrarAsync(evento, usuarioId, detalle);
    }
}
