using System.Text;
using Ingrata.Api;
using Ingrata.Api.Modules.Auth;
using Ingrata.Api.Modules.Catalog;
using Ingrata.Api.Modules.Dashboard;
using Ingrata.Api.Modules.Movements;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Errores;
using Ingrata.Api.Shared.Opciones;
using Ingrata.Api.Shared.Tiempo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cadenaConexion))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:Default. Definela en variables de entorno (ver .env.example).");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Falta Jwt:Key o tiene menos de 32 caracteres. Definelo en variables de entorno (ver .env.example).");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.Seccion));

builder.Services.AddDbContext<IngrataDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<ITiempoServicio, TiempoServicio>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
builder.Services.AddSingleton<MovimientoFactory>();
builder.Services.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "rol",
            NameClaimType = "nombre"
        };

        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var db = contexto.HttpContext.RequestServices.GetRequiredService<IngrataDbContext>();

                var jtiTexto = contexto.Principal?.FindFirst("jti")?.Value;
                if (Guid.TryParse(jtiTexto, out var jti) &&
                    await db.SesionesRevocadas.AnyAsync(s => s.Jti == jti))
                {
                    contexto.Fail("La sesion fue cerrada.");
                    return;
                }

                var usuarioIdTexto = contexto.Principal?.FindFirst("usuario_id")?.Value;
                if (!int.TryParse(usuarioIdTexto, out var usuarioId) ||
                    !await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Activo))
                {
                    contexto.Fail("La cuenta no existe o esta desactivada.");
                }
            },
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                await RespuestasError.EscribirJsonAsync(contexto.HttpContext, 401,
                    CodigosError.NoAutenticado, "Se requiere una sesion valida para acceder.");
            },
            OnForbidden = async contexto =>
            {
                await RespuestasError.EscribirJsonAsync(contexto.HttpContext, 403,
                    CodigosError.SinPermiso, "No tiene permisos para realizar esta operacion.");

                var bitacora = contexto.HttpContext.RequestServices.GetRequiredService<IBitacoraService>();
                await bitacora.RegistrarDesdeContextoAsync(contexto.HttpContext, "acceso_rechazado",
                    $"Acceso prohibido a {contexto.Request.Method} {contexto.Request.Path}.");
            }
        };
    });

builder.Services.AddAuthorization();

var origenFrontend = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200";
builder.Services.AddCors(opciones => opciones.AddPolicy("Frontend", politica => politica
    .WithOrigins(origenFrontend)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers().ConfigureApiBehaviorOptions(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors
                    .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Valor no valido." : x.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new
        {
            codigo = CodigosError.Validacion,
            mensaje = "Los datos enviados no son validos.",
            errores
        });
    };
});

var app = builder.Build();

app.UseMiddleware<ManejadorExcepcionesMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    estado = "ok",
    fechaUtc = DateTime.UtcNow
}));

await SemillaAdminInicial.EjecutarAsync(app.Services);

app.Run();

public partial class Program { }
