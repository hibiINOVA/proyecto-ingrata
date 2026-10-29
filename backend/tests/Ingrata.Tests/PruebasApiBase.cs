using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ingrata.Tests;

[Collection(ColeccionPruebas.Nombre)]
public abstract class PruebasApiBase
{
    protected IngrataApiFactory Fabrica { get; }
    protected HttpClient Api { get; }

    protected PruebasApiBase(IngrataApiFactory fabrica)
    {
        Fabrica = fabrica;
        Api = fabrica.CreateClient();
    }

    protected HttpClient Cliente(string? token = null)
    {
        var cliente = Fabrica.CreateClient();
        if (!string.IsNullOrEmpty(token))
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    protected async Task<string> LoginAdminAsync() =>
        await LoginAsync(IngrataApiFactory.AdminId, IngrataApiFactory.AdminPassword);

    protected async Task<string> LoginAsync(string identificador, string password)
    {
        using var cliente = Cliente();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { identificador, password });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var body = await respuesta.Content.ReadFromJsonAsync<LoginResponseJson>();
        Assert.NotNull(body);
        return body!.Token;
    }

    protected async Task<(string Token, string Identificador)> CrearAlmacenistaAsync(string adminToken)
    {
        var identificador = $"alm-{Guid.NewGuid():N}"[..20];
        const string password = "Alm123456!";

        using var cliente = Cliente(adminToken);
        var respuesta = await cliente.PostAsJsonAsync("/api/usuarios", new
        {
            identificador,
            nombreCompleto = "Almacenista de pruebas",
            password,
            rolId = 2
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var token = await LoginAsync(identificador, password);
        return (token, identificador);
    }

    protected async Task<int> CrearProductoAsync(string adminToken, string? sku = null,
        string unidad = "pieza")
    {
        sku ??= SkuUnico();

        using var admin = Cliente(adminToken);

        var marcas = await admin.GetFromJsonAsync<List<MarcaJson>>("/api/marcas?activo=true");
        var categorias = await admin.GetFromJsonAsync<List<CategoriaJson>>("/api/categorias?activo=true");
        Assert.NotNull(marcas);
        Assert.NotNull(categorias);
        Assert.NotEmpty(marcas!);
        Assert.NotEmpty(categorias!);

        var respuesta = await admin.PostAsJsonAsync("/api/productos", new
        {
            sku,
            nombre = $"Producto {sku}",
            marcaId = marcas![0].Id,
            categoriaId = categorias![0].Id,
            unidad
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var producto = await respuesta.Content.ReadFromJsonAsync<ProductoJson>();
        Assert.NotNull(producto);
        return producto!.Id;
    }

    protected async Task<int> ExistenciaAsync(int productoId)
    {
        using var admin = Cliente(await LoginAdminAsync());
        var productos = await admin.GetFromJsonAsync<List<ProductoJson>>(
            $"/api/productos?texto=");
        var producto = productos!.FirstOrDefault(p => p.Id == productoId);
        Assert.NotNull(producto);
        return producto!.Existencia;
    }

    protected static string SkuUnico() => $"SKU-{Guid.NewGuid():N}"[..20];

    protected static async Task<ErrorJson?> LeerErrorAsync(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<ErrorJson>();
}
