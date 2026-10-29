using System.Security.Claims;
using System.Text.Json;

namespace Ingrata.Api.Shared.Errores;

public static class RespuestasError
{
    public static IResult Crear(HttpContext contexto, int status, string codigo, string mensaje,
        IDictionary<string, string[]>? errores = null)
    {
        contexto.Response.StatusCode = status;
        if (errores is { Count: > 0 })
            return Results.Json(new { codigo, mensaje, errores });
        return Results.Json(new { codigo, mensaje });
    }

    public static async Task EscribirJsonAsync(HttpContext contexto, int status, string codigo, string mensaje)
    {
        if (contexto.Response.HasStarted)
            return;
        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/json";
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(new { codigo, mensaje }));
    }
}
