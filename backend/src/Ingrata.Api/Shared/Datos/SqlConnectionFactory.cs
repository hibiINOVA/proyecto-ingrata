using System.Data;
using Microsoft.Data.SqlClient;

namespace Ingrata.Api.Shared.Datos;

public interface ISqlConnectionFactory
{
    IDbConnection Crear();
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _cadena;

    public SqlConnectionFactory(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:Default. Definela en variables de entorno (.env).");
    }

    public IDbConnection Crear() => new SqlConnection(_cadena);
}
