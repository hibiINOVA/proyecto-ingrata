namespace Ingrata.Api.Shared.Errores;

public static class CodigosError
{
    public const string Validacion = "VALIDACION";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string SinPermiso = "SIN_PERMISO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string Conflicto = "CONFLICTO";
    public const string SaldoInsuficiente = "SALDO_INSUFICIENTE";
    public const string DatosInvalidos = "DATOS_INVALIDOS";
    public const string YaAnulado = "YA_ANULADO";
    public const string ModificacionProhibida = "MODIFICACION_PROHIBIDA";
    public const string ErrorInterno = "ERROR_INTERNO";
}

public sealed class ExcepcionDeNegocio : Exception
{
    public string Codigo { get; }
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errores { get; }

    public ExcepcionDeNegocio(string codigo, string mensaje, int statusCode = 400,
        IDictionary<string, string[]>? errores = null)
        : base(mensaje)
    {
        Codigo = codigo;
        StatusCode = statusCode;
        Errores = errores;
    }

    public static ExcepcionDeNegocio NoEncontrado(string mensaje) =>
        new(CodigosError.NoEncontrado, mensaje, 404);

    public static ExcepcionDeNegocio Validacion(string mensaje, IDictionary<string, string[]>? errores = null) =>
        new(CodigosError.Validacion, mensaje, 400, errores);

    public static ExcepcionDeNegocio Conflicto(string mensaje) =>
        new(CodigosError.Conflicto, mensaje, 409);

    public static ExcepcionDeNegocio SinPermiso(string mensaje) =>
        new(CodigosError.SinPermiso, mensaje, 403);
}
