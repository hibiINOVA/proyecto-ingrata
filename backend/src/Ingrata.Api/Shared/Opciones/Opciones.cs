namespace Ingrata.Api.Shared.Opciones;

public sealed class JwtOptions
{
    public const string Seccion = "Jwt";
    public string Key { get; set; } = "";
    public int ExpireMinutes { get; set; } = 30;
    public string Issuer { get; set; } = "InventarioIngrata";
    public string Audience { get; set; } = "InventarioIngrata";
}

public sealed class AppOptions
{
    public const string Seccion = "App";
    public string TimeZone { get; set; } = "America/Mexico_City";
    public bool PermitirMermaAlmacenista { get; set; } = true;
}
