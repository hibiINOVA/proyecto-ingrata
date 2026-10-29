using Ingrata.Api.Shared.Errores;

namespace Ingrata.Api.Modules.Movements;

/// <summary>Crea el movimiento concreto según el tipo solicitado (patrón Factory).</summary>
public sealed class MovimientoFactory
{
    private readonly IReadOnlyDictionary<byte, Func<IMovimiento>> _tipos;

    public MovimientoFactory()
    {
        _tipos = new Dictionary<byte, Func<IMovimiento>>
        {
            [1] = () => new Entrada(),
            [2] = () => new Entrega(),
            [3] = () => new Merma()
        };
    }

    public IMovimiento Crear(byte tipo)
    {
        if (_tipos.TryGetValue(tipo, out var creador))
            return creador();

        throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
            "Tipo de movimiento no soportado.", 400);
    }

    public IMovimiento CrearAnulacion(byte tipoOriginal) => new Reversion(tipoOriginal);
}
