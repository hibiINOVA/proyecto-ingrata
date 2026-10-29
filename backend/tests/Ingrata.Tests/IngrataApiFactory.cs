using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Ingrata.Tests;

/// <summary>Fabrica compartida: SQL Server real (Testcontainers) + API contra esa base.</summary>
public sealed class IngrataApiFactory : WebApplicationFactory<Program>
{
    private readonly MsSqlContainer _contenedor = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Ing_rata2026!")
        .Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public string CadenaConexionIngrata =>
        new SqlConnectionStringBuilder(CadenaConexion) { InitialCatalog = "Ingrata" }.ConnectionString;

    public const string AdminId = "admin";
    public const string AdminPassword = "Admin12345!";

    public IngrataApiFactory()
    {
        _contenedor.StartAsync().GetAwaiter().GetResult();
        EjecutarScriptsEsquemaAsync(CadenaConexion).GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", CadenaConexionIngrata);
        builder.UseSetting("Jwt:Key", "clave-de-pruebas-suficientemente-larga-0123456789abcdef");
        builder.UseSetting("Jwt:ExpireMinutes", "30");
        builder.UseSetting("Jwt:Issuer", "InventarioIngrata");
        builder.UseSetting("Jwt:Audience", "InventarioIngrata");
        builder.UseSetting("App:TimeZone", "America/Mexico_City");
        builder.UseSetting("App:PermitirMermaAlmacenista", "true");
        builder.UseSetting("BOOTSTRAP_ADMIN_ID", AdminId);
        builder.UseSetting("BOOTSTRAP_ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("Cors:AllowedOrigin", "http://localhost:4200");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _contenedor.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static async Task EjecutarScriptsEsquemaAsync(string cadenaMaestro)
    {
        var maestro = new SqlConnectionStringBuilder(cadenaMaestro) { InitialCatalog = "master" }.ConnectionString;

        await using (var conexion = new SqlConnection(maestro))
        {
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(
                "IF DB_ID('Ingrata') IS NULL CREATE DATABASE Ingrata;" +
                "ALTER DATABASE Ingrata SET QUOTED_IDENTIFIER ON;", conexion);
            await comando.ExecuteNonQueryAsync();
        }

        var baseDatos = new SqlConnectionStringBuilder(cadenaMaestro) { InitialCatalog = "Ingrata" }.ConnectionString;

        foreach (var archivo in ArchivosDeScripts())
        {
            var contenido = await File.ReadAllTextAsync(archivo);
            var lotes = Regex.Split(contenido, @"^\s*GO\s*;?\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            await using var conexion = new SqlConnection(baseDatos);
            await conexion.OpenAsync();

            foreach (var lote in lotes)
            {
                if (string.IsNullOrWhiteSpace(lote))
                    continue;
                await using var comando = new SqlCommand(lote, conexion);
                await comando.ExecuteNonQueryAsync();
            }
        }
    }

    private static IEnumerable<string> ArchivosDeScripts()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            var carpetaDb = Path.Combine(directorio.FullName, "db");
            if (Directory.Exists(carpetaDb) && File.Exists(Path.Combine(carpetaDb, "001_schema.sql")))
            {
                yield return Path.Combine(carpetaDb, "001_schema.sql");
                yield return Path.Combine(carpetaDb, "002_procedures.sql");
                yield return Path.Combine(carpetaDb, "003_triggers.sql");
                yield return Path.Combine(carpetaDb, "004_seed.sql");
                yield break;
            }
            directorio = directorio.Parent;
        }
        throw new InvalidOperationException("No se encontró la carpeta db/ con los scripts del esquema.");
    }
}

[CollectionDefinition(ColeccionPruebas.Nombre)]
public class ColeccionPruebas : ICollectionFixture<IngrataApiFactory>
{
    public const string Nombre = "ingrata";
}
