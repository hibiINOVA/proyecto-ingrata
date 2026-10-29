using System.Data;
using Dapper;
using Ingrata.Api.Modules.Movements.Dtos;
using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Movements;

public class RegistroMovimiento
{
    public byte Tipo { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public int UsuarioId { get; set; }
    public string? Proveedor { get; set; }
    public string? Referencia { get; set; }
    public string? Destino { get; set; }
    public string? Motivo { get; set; }
    public Guid? RequestId { get; set; }
}

public interface IMovimientoRepositorio
{
    Task<string> RegistrarAsync(RegistroMovimiento registro);
    Task<string> AnularAsync(long movimientoId, int usuarioId, string motivo);
    Task<InfoMovimiento?> ObtenerInfoAsync(long movimientoId);
    Task<int?> ObtenerExistenciaAsync(int productoId);
    Task<long?> ObtenerIdPorRequestIdAsync(Guid requestId);
    Task<long> ObtenerIdPorFolioAsync(string folio);
    Task<RespuestaPaginada<MovimientoDto>> ListarHistorialAsync(FiltroMovimientos filtros);
}

public record InfoMovimiento(long Id, byte Tipo, int ProductoId, string Folio, int Cantidad);

public sealed class MovimientoRepositorio : IMovimientoRepositorio
{
    private const int TamanoPagina = 20;
    private readonly ISqlConnectionFactory _factory;
    private readonly IngrataDbContext _db;

    public MovimientoRepositorio(ISqlConnectionFactory factory, IngrataDbContext db)
    {
        _factory = factory;
        _db = db;
    }

    public async Task<string> RegistrarAsync(RegistroMovimiento registro)
    {
        using var conexion = _factory.Crear();
        var parametros = new DynamicParameters();
        parametros.Add("@Tipo", registro.Tipo);
        parametros.Add("@ProductoId", registro.ProductoId);
        parametros.Add("@Cantidad", registro.Cantidad);
        parametros.Add("@UsuarioId", registro.UsuarioId);
        parametros.Add("@Proveedor", registro.Proveedor);
        parametros.Add("@Referencia", registro.Referencia);
        parametros.Add("@Destino", registro.Destino);
        parametros.Add("@Motivo", registro.Motivo);
        parametros.Add("@RequestId", registro.RequestId);
        parametros.Add("@Folio", direction: ParameterDirection.Output, size: 20);

        await conexion.ExecuteAsync("dbo.sp_RegistrarMovimiento", parametros,
            commandType: CommandType.StoredProcedure);

        return parametros.Get<string>("@Folio");
    }

    public async Task<string> AnularAsync(long movimientoId, int usuarioId, string motivo)
    {
        using var conexion = _factory.Crear();
        var parametros = new DynamicParameters();
        parametros.Add("@MovimientoId", movimientoId);
        parametros.Add("@UsuarioId", usuarioId);
        parametros.Add("@Motivo", motivo);
        parametros.Add("@Folio", direction: ParameterDirection.Output, size: 20);

        await conexion.ExecuteAsync("dbo.sp_AnularMovimiento", parametros,
            commandType: CommandType.StoredProcedure);

        return parametros.Get<string>("@Folio");
    }

    public Task<InfoMovimiento?> ObtenerInfoAsync(long movimientoId) =>
        _db.Movimientos.AsNoTracking()
            .Where(m => m.Id == movimientoId)
            .Select(m => new InfoMovimiento(m.Id, m.Tipo, m.ProductoId, m.Folio, m.Cantidad))
            .FirstOrDefaultAsync();

    public Task<int?> ObtenerExistenciaAsync(int productoId) =>
        _db.SaldosProducto.AsNoTracking()
            .Where(s => s.ProductoId == productoId)
            .Select(s => (int?)s.Existencia)
            .FirstOrDefaultAsync();

    public Task<long?> ObtenerIdPorRequestIdAsync(Guid requestId) =>
        _db.Movimientos.AsNoTracking()
            .Where(m => m.RequestId == requestId)
            .Select(m => (long?)m.Id)
            .FirstOrDefaultAsync();

    public Task<long> ObtenerIdPorFolioAsync(string folio) =>
        _db.Movimientos.AsNoTracking()
            .Where(m => m.Folio == folio)
            .Select(m => m.Id)
            .FirstAsync();

    public async Task<RespuestaPaginada<MovimientoDto>> ListarHistorialAsync(FiltroMovimientos filtros)
    {
        var consulta = _db.Movimientos.AsNoTracking();

        var desde = NormalizarDesde(filtros.Desde);
        var hasta = NormalizarHasta(filtros.Hasta);

        if (desde.HasValue)
            consulta = consulta.Where(m => m.FechaUtc >= desde.Value);

        if (hasta.HasValue)
            consulta = consulta.Where(m => m.FechaUtc < hasta.Value);

        if (filtros.ProductoId.HasValue)
            consulta = consulta.Where(m => m.ProductoId == filtros.ProductoId.Value);

        if (filtros.Tipo.HasValue)
            consulta = consulta.Where(m => m.Tipo == filtros.Tipo.Value);

        if (filtros.UsuarioId.HasValue)
            consulta = consulta.Where(m => m.UsuarioId == filtros.UsuarioId.Value);

        var total = await consulta.CountAsync();

        var pagina = Math.Max(1, filtros.Pagina);

        var elementos = await consulta
            .OrderByDescending(m => m.FechaUtc)
            .ThenByDescending(m => m.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(m => new MovimientoDto
            {
                Id = m.Id,
                Folio = m.Folio,
                Tipo = m.Tipo,
                TipoNombre =
                    m.Tipo == 1 ? "Entrada" :
                    m.Tipo == 2 ? "Entrega" :
                    m.Tipo == 3 ? "Merma" : "Reversión",
                ProductoId = m.ProductoId,
                Sku = m.SkuSnapshot,
                ProductoNombre = m.Producto!.Nombre,
                Unidad = m.UnidadSnapshot,
                Cantidad = m.Cantidad,
                UsuarioId = m.UsuarioId,
                UsuarioNombre = m.Usuario!.NombreCompleto,
                FechaUtc = m.FechaUtc,
                Proveedor = m.Proveedor,
                Referencia = m.Referencia,
                Destino = m.Destino,
                Motivo = m.Motivo,
                MovimientoOriginalId = m.MovimientoOriginalId,
                FolioOriginal = m.MovimientoOriginal != null ? m.MovimientoOriginal.Folio : null,
                Anulado = _db.Movimientos.Any(x => x.Tipo == 4 && x.MovimientoOriginalId == m.Id)
            })
            .ToListAsync();

        foreach (var movimiento in elementos)
            movimiento.FechaUtc = DateTime.SpecifyKind(movimiento.FechaUtc, DateTimeKind.Utc);

        return new RespuestaPaginada<MovimientoDto>
        {
            Pagina = pagina,
            Tamano = TamanoPagina,
            Total = total,
            Elementos = elementos
        };
    }

    private static DateTime? NormalizarDesde(DateTime? desde)
    {
        if (!desde.HasValue)
            return null;
        var valor = desde.Value;
        return valor.TimeOfDay == TimeSpan.Zero
            ? DateTime.SpecifyKind(valor.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
    }

    private static DateTime? NormalizarHasta(DateTime? hasta)
    {
        if (!hasta.HasValue)
            return null;
        var valor = hasta.Value;
        var limite = valor.TimeOfDay == TimeSpan.Zero
            ? valor.Date.AddDays(1)
            : valor;
        return DateTime.SpecifyKind(limite, DateTimeKind.Utc);
    }
}
