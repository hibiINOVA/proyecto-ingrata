namespace Ingrata.Api.Shared.Tiempo;

public interface ITiempoServicio
{
    DateTime AhoraUtc { get; }
    string ZonaHoraria { get; }
    DateTime ALocal(DateTime fechaUtc);
    string FormatearLocal(DateTime fechaUtc);
}

public sealed class TiempoServicio : ITiempoServicio
{
    private readonly TimeZoneInfo _zona;

    public TiempoServicio(IConfiguration configuracion)
    {
        ZonaHoraria = configuracion["App:TimeZone"] ?? "America/Mexico_City";
        try
        {
            _zona = TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria);
        }
        catch (TimeZoneNotFoundException)
        {
            _zona = TimeZoneInfo.Utc;
            ZonaHoraria = "UTC";
        }
    }

    public DateTime AhoraUtc => DateTime.UtcNow;
    public string ZonaHoraria { get; }

    public DateTime ALocal(DateTime fechaUtc)
    {
        var utc = fechaUtc.Kind == DateTimeKind.Utc
            ? fechaUtc
            : DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _zona);
    }

    public string FormatearLocal(DateTime fechaUtc) =>
        ALocal(fechaUtc).ToString("yyyy-MM-dd'T'HH:mm:ss");
}
