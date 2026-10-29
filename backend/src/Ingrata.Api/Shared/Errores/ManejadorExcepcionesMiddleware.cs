using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Shared.Errores;

public sealed class ManejadorExcepcionesMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorExcepcionesMiddleware> _logger;

    public ManejadorExcepcionesMiddleware(RequestDelegate siguiente,
        ILogger<ManejadorExcepcionesMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            await ManejarAsync(contexto, ex);
        }
    }

    private async Task ManejarAsync(HttpContext contexto, Exception ex)
    {
        var (status, codigo, mensaje) = Traducir(ex);

        if (status >= 500)
            _logger.LogError(ex, "Error no controlado al atender {Metodo} {Ruta}",
                contexto.Request.Method, contexto.Request.Path);
        else
            _logger.LogWarning(ex, "Rechazo de negocio {Codigo} en {Metodo} {Ruta}",
                codigo, contexto.Request.Method, contexto.Request.Path);

        if (contexto.Response.HasStarted)
            throw ex;

        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/json";
        await contexto.Response.WriteAsJsonAsync(new
        {
            codigo,
            mensaje
        });
    }

    internal static (int Status, string Codigo, string Mensaje) Traducir(Exception ex)
    {
        switch (ex)
        {
            case ExcepcionDeNegocio negocio:
                return (negocio.StatusCode, negocio.Codigo, negocio.Message);

            case SqlException sql:
                return TraducirSql(sql.Number, sql);

            case DbUpdateException { InnerException: SqlException sqlInterior }:
                return TraducirSql(sqlInterior.Number, sqlInterior);

            case DbUpdateException dbUpdate:
                _ = dbUpdate;
                return (409, CodigosError.Conflicto, "La operacion entra en conflicto con los datos existentes.");
        }

        if (ex.InnerException is SqlException sqlAnidada)
            return TraducirSql(sqlAnidada.Number, sqlAnidada);

        return (500, CodigosError.ErrorInterno, "Ocurrio un error inesperado. Intente de nuevo.");
    }

    private static (int, string, string) TraducirNumeroSql(int numero) => numero switch
    {
        50001 => (409, CodigosError.SaldoInsuficiente, "Saldo insuficiente para completar la operacion."),
        50002 => (400, CodigosError.DatosInvalidos, "Los datos enviados no son validos."),
        50003 => (409, CodigosError.YaAnulado, "El movimiento ya fue anulado o no puede anularse."),
        50004 => (404, CodigosError.NoEncontrado, "El recurso solicitado no existe o esta inactivo."),
        50010 => (409, CodigosError.ModificacionProhibida, "Los registros son inmutables y no pueden modificarse."),
        2601 or 2627 => (409, CodigosError.Conflicto, "Ya existe un registro con el mismo valor unico."),
        547 => (409, CodigosError.Conflicto, "La operacion entra en conflicto con registros relacionados."),
        _ => (500, CodigosError.ErrorInterno, "Ocurrio un error inesperado. Intente de nuevo.")
    };

    private static (int, string, string) TraducirSql(int numero, SqlException sql)
    {
        if (numero is 50001 or 50002 or 50003 or 50004 or 50010 or 2601 or 2627 or 547)
            return TraducirNumeroSql(numero);

        return TraducirNumeroSql(-1);
    }
}
