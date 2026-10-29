using Ingrata.Api.Modules.Movements.Dtos;
using Ingrata.Api.Shared.Errores;

namespace Ingrata.Api.Modules.Movements;

/// <summary>
/// Estrategia de un tipo de movimiento: expone el signo de su efecto
/// sobre la existencia y valida sus datos propios (patrón Strategy).
/// </summary>
public interface IMovimiento
{
    byte Tipo { get; }
    string Nombre { get; }

    /// <summary>+1 aumenta la existencia, -1 la disminuye.</summary>
    int Signo { get; }

    void ValidarDatos(SolicitudMovimiento solicitud);
}

public sealed class Entrada : IMovimiento
{
    public byte Tipo => 1;
    public string Nombre => "Entrada";
    public int Signo => 1;

    public void ValidarDatos(SolicitudMovimiento solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Proveedor))
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "La entrada requiere proveedor.", 400,
                new Dictionary<string, string[]>
                {
                    ["proveedor"] = new[] { "El proveedor es obligatorio para entradas." }
                });
    }
}

public sealed class Entrega : IMovimiento
{
    public byte Tipo => 2;
    public string Nombre => "Entrega";
    public int Signo => -1;

    public void ValidarDatos(SolicitudMovimiento solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Destino))
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "La entrega requiere destino.", 400,
                new Dictionary<string, string[]>
                {
                    ["destino"] = new[] { "El destino es obligatorio para entregas." }
                });
    }
}

public sealed class Merma : IMovimiento
{
    public byte Tipo => 3;
    public string Nombre => "Merma";
    public int Signo => -1;

    public void ValidarDatos(SolicitudMovimiento solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "La merma requiere motivo.", 400,
                new Dictionary<string, string[]>
                {
                    ["motivo"] = new[] { "El motivo es obligatorio para mermas." }
                });
    }
}

/// <summary>Reversión de un movimiento original: su efecto es −Δ(original).</summary>
public sealed class Reversion : IMovimiento
{
    private readonly byte _tipoOriginal;

    public Reversion(byte tipoOriginal) => _tipoOriginal = tipoOriginal;

    public byte Tipo => 4;
    public string Nombre => "Reversión";
    public int Signo => _tipoOriginal == 1 ? -1 : 1;

    public void ValidarDatos(SolicitudMovimiento solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "La anulación requiere motivo.", 400,
                new Dictionary<string, string[]>
                {
                    ["motivo"] = new[] { "El motivo de anulación es obligatorio." }
                });
    }
}
