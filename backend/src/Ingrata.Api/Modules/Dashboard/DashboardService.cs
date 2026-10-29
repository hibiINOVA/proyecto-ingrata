using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Dashboard;

public class FiltroDashboard
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class ExistenciaProductoDto
{
    public int ProductoId { get; set; }
    public string Sku { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Unidad { get; set; } = "";
    public int Existencia { get; set; }
    public bool Activo { get; set; }
}

public class TotalesPeriodoDto
{
    public int Entradas { get; set; }
    public int Entregas { get; set; }
    public int Mermas { get; set; }
}

public class DashboardDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int TotalExistencias { get; set; }
    public List<ExistenciaProductoDto> Existencias { get; set; } = new();
    public TotalesPeriodoDto Totales { get; set; } = new();
    public DateTime AhoraUtc { get; set; }
    public DateTime AhoraLocal { get; set; }
    public string ZonaHoraria { get; set; } = "";
}

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync(FiltroDashboard filtro);
}

public sealed class DashboardService : IDashboardService
{
    private readonly IngrataDbContext _db;
    private readonly Shared.Tiempo.ITiempoServicio _tiempo;

    public DashboardService(IngrataDbContext db, Shared.Tiempo.ITiempoServicio tiempo)
    {
        _db = db;
        _tiempo = tiempo;
    }

    public async Task<DashboardDto> ObtenerAsync(FiltroDashboard filtro)
    {
        var desde = NormalizarDesde(filtro.Desde);
        var hasta = NormalizarHasta(filtro.Hasta);

        var existencias = await _db.SaldosProducto.AsNoTracking()
            .Include(s => s.Producto)
            .Where(s => s.Existencia != 0)
            .OrderByDescending(s => s.Existencia)
            .Select(s => new ExistenciaProductoDto
            {
                ProductoId = s.ProductoId,
                Sku = s.Producto!.Sku,
                Nombre = s.Producto.Nombre,
                Unidad = s.Producto.Unidad,
                Existencia = s.Existencia,
                Activo = s.Producto.Activo
            })
            .ToListAsync();

        var movimientos = _db.Movimientos.AsNoTracking().AsQueryable();

        if (desde.HasValue)
            movimientos = movimientos.Where(m => m.FechaUtc >= desde.Value);

        if (hasta.HasValue)
            movimientos = movimientos.Where(m => m.FechaUtc < hasta.Value);

        async Task<int> SumaAsync(IQueryable<MovimientoEntity> consulta) =>
            await consulta.SumAsync(m => (int?)m.Cantidad) ?? 0;

        var entradasBrutas = await SumaAsync(movimientos.Where(m => m.Tipo == 1));
        var entradasRevertidas = await SumaAsync(
            movimientos.Where(m => m.Tipo == 4 && m.MovimientoOriginal!.Tipo == 1));

        var entregasBrutas = await SumaAsync(movimientos.Where(m => m.Tipo == 2));
        var entregasRevertidas = await SumaAsync(
            movimientos.Where(m => m.Tipo == 4 && m.MovimientoOriginal!.Tipo == 2));

        var mermasBrutas = await SumaAsync(movimientos.Where(m => m.Tipo == 3));
        var mermasRevertidas = await SumaAsync(
            movimientos.Where(m => m.Tipo == 4 && m.MovimientoOriginal!.Tipo == 3));

        var totales = new TotalesPeriodoDto
        {
            Entradas = entradasBrutas - entradasRevertidas,
            Entregas = entregasBrutas - entregasRevertidas,
            Mermas = mermasBrutas - mermasRevertidas
        };

        var ahoraUtc = _tiempo.AhoraUtc;

        return new DashboardDto
        {
            Desde = desde,
            Hasta = hasta,
            TotalExistencias = existencias.Sum(e => e.Existencia),
            Existencias = existencias,
            Totales = totales,
            AhoraUtc = ahoraUtc,
            AhoraLocal = _tiempo.ALocal(ahoraUtc),
            ZonaHoraria = _tiempo.ZonaHoraria
        };
    }

    private static DateTime? NormalizarDesde(DateTime? desde)
    {
        if (!desde.HasValue)
            return null;
        var valor = desde.Value;
        return DateTime.SpecifyKind(
            valor.TimeOfDay == TimeSpan.Zero ? valor.Date : valor, DateTimeKind.Utc);
    }

    private static DateTime? NormalizarHasta(DateTime? hasta)
    {
        if (!hasta.HasValue)
            return null;
        var valor = hasta.Value;
        var limite = valor.TimeOfDay == TimeSpan.Zero ? valor.Date.AddDays(1) : valor;
        return DateTime.SpecifyKind(limite, DateTimeKind.Utc);
    }
}
