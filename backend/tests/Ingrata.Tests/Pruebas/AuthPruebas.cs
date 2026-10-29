using System.Net;
using System.Net.Http.Json;

namespace Ingrata.Tests.Pruebas;

public class AuthPruebas : PruebasApiBase
{
    public AuthPruebas(IngrataApiFactory fabrica) : base(fabrica) { }

    [Fact(DisplayName = "PT-01: login con credenciales activas devuelve JWT")]
    public async Task PT01_LoginValido()
    {
        var token = await LoginAdminAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var cliente = Cliente(token);
        var respuesta = await cliente.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact(DisplayName = "PT-02: login con credenciales invalidas es rechazado")]
    public async Task PT02_LoginInvalido()
    {
        using var cliente = Cliente();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { identificador = IngrataApiFactory.AdminId, password = "no-correcta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var error = await LeerErrorAsync(respuesta);
        Assert.NotNull(error);
        Assert.Equal("NO_AUTENTICADO", error!.Codigo);
    }

    [Fact(DisplayName = "PT-02: el token queda inutilizable tras el logout")]
    public async Task PT02_LogoutInvalidaToken()
    {
        var token = await LoginAdminAsync();

        using var cliente = Cliente(token);
        var logout = await cliente.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        using var despues = Cliente(token);
        var respuesta = await despues.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact(DisplayName = "PT-03: sin token la API responde 401")]
    public async Task PT03_SinToken_401()
    {
        using var cliente = Cliente();
        var respuesta = await cliente.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact(DisplayName = "PT-03: Almacenista recibe 403 en rutas de catálogo, usuarios y anulación")]
    public async Task PT03_Almacenista_403()
    {
        var admin = await LoginAdminAsync();
        var (almacenista, _) = await CrearAlmacenistaAsync(admin);
        using var cliente = Cliente(almacenista);

        var usuarios = await cliente.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, usuarios.StatusCode);
        var errorUsuarios = await LeerErrorAsync(usuarios);
        Assert.Equal("SIN_PERMISO", errorUsuarios!.Codigo);

        var marcas = await cliente.PostAsJsonAsync("/api/marcas", new { nombre = $"Marca-{Guid.NewGuid():N}"[..20] });
        Assert.Equal(HttpStatusCode.Forbidden, marcas.StatusCode);

        var anulacion = await cliente.PostAsJsonAsync("/api/movimientos/1/anulacion", new { motivo = "prueba" });
        Assert.Equal(HttpStatusCode.Forbidden, anulacion.StatusCode);

        var bitacora = await cliente.GetAsync("/api/bitacora");
        Assert.Equal(HttpStatusCode.Forbidden, bitacora.StatusCode);

        using var adminCliente = Cliente(admin);
        var marcasAdmin = await adminCliente.GetFromJsonAsync<List<MarcaJson>>("/api/marcas");
        Assert.DoesNotContain(marcasAdmin!, m => m.Nombre.StartsWith("Marca-"));
    }

    [Fact(DisplayName = "HU-04: una cuenta desactivada pierde acceso")]
    public async Task CuentaDesactivada_PierdeAcceso()
    {
        var admin = await LoginAdminAsync();
        var (almacenistaToken, identificador) = await CrearAlmacenistaAsync(admin);

        using var adminCliente = Cliente(admin);
        var lista = await adminCliente.GetFromJsonAsync<List<UsuarioJson>>("/api/usuarios");
        var usuario = lista!.Single(u => u.Identificador == identificador);

        var patch = await adminCliente.PatchAsJsonAsync($"/api/usuarios/{usuario.Id}/estado",
            new { activo = false });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var respuesta = await Cliente(almacenistaToken).GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var reintentoLogin = await Cliente().PostAsJsonAsync("/api/auth/login",
            new { identificador, password = "Alm123456!" });
        Assert.Equal(HttpStatusCode.Unauthorized, reintentoLogin.StatusCode);
    }

    [Fact(DisplayName = "HU-04: no se puede desactivar la propia cuenta ni al último administrador")]
    public async Task ProteccionUltimoAdmin()
    {
        var admin = await LoginAdminAsync();
        using var adminCliente = Cliente(admin);
        var lista = await adminCliente.GetFromJsonAsync<List<UsuarioJson>>("/api/usuarios");
        var yo = lista!.Single(u => u.Identificador == IngrataApiFactory.AdminId);

        var propia = await adminCliente.PatchAsJsonAsync($"/api/usuarios/{yo.Id}/estado",
            new { activo = false });
        Assert.Equal(HttpStatusCode.Conflict, propia.StatusCode);
    }
}
