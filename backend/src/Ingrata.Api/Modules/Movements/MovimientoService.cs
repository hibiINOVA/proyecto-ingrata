using Ingrata.Api.Modules.Movements.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Dtos;
using Ingrata.Api.Shared.Errores;
using Ingrata.Api.Shared.Opciones;
using Microsoft.Extensions.Options;

namespace Ingrata.Api.Modules.Movements;

public interface IMovimientoService
{
    Task<ResultadoMovimiento> RegistrarAsync(byte tipo, SolicitudMovimiento solicitud,
        int usuarioId, string rol);
    Task<ResultadoMovimiento> AnularAsync(long movimientoId, string motivo, int usuarioId);
    Task<RespuestaPaginada<MovimientoDto>> HistorialAsync(FiltroMovimientos filtros,
        int usuarioId, bool esAdministrador);
}

/// <summary>Orquesta las reglas de negocio de movimientos; no persiste ni audita por su cuenta.</summary>
public sealed class MovimientoService : IMovimientoService
{
    private readonly MovimientoFactory _factory;
    private readonly IMovimientoRepositorio _repositorio;
    private readonly IBitacoraService _bitacora;
    private readonly AppOptions _opciones;

    public MovimientoService(MovimientoFactory factory, IMovimientoRepositorio repositorio,
        IBitacoraService bitacora, IOptions<AppOptions> opciones)
    {
        _factory = factory;
        _repositorio = repositorio;
        _bitacora = bitacora;
        _opciones = opciones.Value;
    }

    public async Task<ResultadoMovimiento> RegistrarAsync(byte tipo, SolicitudMovimiento solicitud,
        int usuarioId, string rol)
    {
        var movimiento = _factory.Crear(tipo);
        movimiento.ValidarDatos(solicitud);

        if (tipo == 3 && rol != "Administrador" && !_opciones.PermitirMermaAlmacenista)
            throw ExcepcionDeNegocio.SinPermiso(
                "Su rol no tiene autorizacion para registrar mermas.");

        var folio = await _repositorio.RegistrarAsync(new RegistroMovimiento
        {
            Tipo = movimiento.Tipo,
            ProductoId = solicitud.ProductoId,
            Cantidad = solicitud.Cantidad,
            UsuarioId = usuarioId,
            Proveedor = Normalizar(solicitud.Proveedor),
            Referencia = Normalizar(solicitud.Referencia),
            Destino = Normalizar(solicitud.Destino),
            Motivo = Normalizar(solicitud.Motivo),
            RequestId = solicitud.RequestId
        });

        var existencia = await _repositorio.ObtenerExistenciaAsync(solicitud.ProductoId);
        var movimientoId = await _repositorio.ObtenerIdPorFolioAsync(folio);

        return new ResultadoMovimiento
        {
            Id = movimientoId,
            Folio = folio,
            Tipo = movimiento.Tipo,
            TipoNombre = movimiento.Nombre,
            Efecto = movimiento.Signo * solicitud.Cantidad,
            ExistenciaResultante = existencia,
            Mensaje = $"{movimiento.Nombre} registrada correctamente."
        };
    }

    public async Task<ResultadoMovimiento> AnularAsync(long movimientoId, string motivo, int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "El motivo de anulacion es obligatorio.", 400,
                new Dictionary<string, string[]>
                {
                    ["motivo"] = new[] { "El motivo de anulacion es obligatorio." }
                });

        var info = await _repositorio.ObtenerInfoAsync(movimientoId);
        if (info is null)
            throw ExcepcionDeNegocio.NoEncontrado("El movimiento no existe.");

        if (info.Tipo == 4)
            throw new ExcepcionDeNegocio(CodigosError.YaAnulado,
                "No se puede anular una anulacion.", 409);

        var reversion = _factory.CrearAnulacion(info.Tipo);
        reversion.ValidarDatos(new SolicitudMovimiento { Motivo = motivo });

        var folio = await _repositorio.AnularAsync(movimientoId, usuarioId, motivo.Trim());
        var existencia = await _repositorio.ObtenerExistenciaAsync(info.ProductoId);

        await _bitacora.RegistrarAsync("anulacion", usuarioId,
            $"Anulacion del movimiento {movimientoId} ({info.Folio}) con folio {folio}.");

        return new ResultadoMovimiento
        {
            Id = movimientoId,
            Folio = folio,
            Tipo = reversion.Tipo,
            TipoNombre = reversion.Nombre,
            Efecto = reversion.Signo * info.Cantidad,
            ExistenciaResultante = existencia,
            Mensaje = $"Anulacion registrada con folio {folio}."
        };
    }

    public Task<RespuestaPaginada<MovimientoDto>> HistorialAsync(FiltroMovimientos filtros,
        int usuarioId, bool esAdministrador)
    {
        if (!esAdministrador && filtros.UsuarioId.HasValue && filtros.UsuarioId.Value != usuarioId)
            throw ExcepcionDeNegocio.SinPermiso(
                "Solo el administrador puede filtrar el historial de otros usuarios.");

        return _repositorio.ListarHistorialAsync(filtros);
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
