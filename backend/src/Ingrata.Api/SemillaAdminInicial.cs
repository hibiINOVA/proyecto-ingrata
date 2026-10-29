using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Validacion;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api;

/// <summary>RD-05: alta inicial del Administrador desde variables de entorno.</summary>
public static class SemillaAdminInicial
{
    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var logger = alcance.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SemillaAdminInicial");

        try
        {
            var db = alcance.ServiceProvider.GetRequiredService<IngrataDbContext>();
            var configuracion = alcance.ServiceProvider.GetRequiredService<IConfiguration>();

            if (await db.Usuarios.AnyAsync(u => u.RolId == 1))
                return;

            var identificador = configuracion["BOOTSTRAP_ADMIN_ID"];
            var password = configuracion["BOOTSTRAP_ADMIN_PASSWORD"];

            if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "No existe ningun Administrador y faltan BOOTSTRAP_ADMIN_ID / BOOTSTRAP_ADMIN_PASSWORD. " +
                    "No se creo ninguna cuenta.");
                return;
            }

            if (await db.Usuarios.AnyAsync(u => u.Identificador == identificador.Trim()))
            {
                logger.LogWarning(
                    "El identificador BOOTSTRAP_ADMIN_ID '{Id}' ya existe; no se creo la cuenta.",
                    identificador);
                return;
            }

            db.Usuarios.Add(new UsuarioEntity
            {
                Identificador = identificador.Trim(),
                NombreCompleto = "Administrador",
                PasswordHash = PasswordHasher.Hash(password),
                RolId = 1,
                Activo = true,
                CreadoUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            logger.LogInformation("Administrador inicial '{Id}' creado (RD-05).", identificador.Trim());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo ejecutar la semilla del administrador inicial.");
        }
    }
}
