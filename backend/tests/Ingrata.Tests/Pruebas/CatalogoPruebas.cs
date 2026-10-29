using System.Net;
using System.Net.Http.Json;

namespace Ingrata.Tests.Pruebas;

public class CatalogoPruebas : PruebasApiBase
{
    public CatalogoPruebas(IngrataApiFactory fabrica) : base(fabrica) { }

    [Fact(DisplayName = "PT-06: SKU duplicado es rechazado con 409")]
    public async Task PT06_SkuDuplicado()
    {
        var admin = await LoginAdminAsync();
        var sku = SkuUnico();
        await CrearProductoAsync(admin, sku);

        using var cliente = Cliente(admin);
        var marcas = await cliente.GetFromJsonAsync<List<MarcaJson>>("/api/marcas?activo=true");
        var categorias = await cliente.GetFromJsonAsync<List<CategoriaJson>>("/api/categorias?activo=true");

        var respuesta = await cliente.PostAsJsonAsync("/api/productos", new
        {
            sku,
            nombre = "Otro producto",
            marcaId = marcas![0].Id,
            categoriaId = categorias![0].Id,
            unidad = "pieza"
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var error = await LeerErrorAsync(respuesta);
        Assert.Equal("CONFLICTO", error!.Codigo);
    }

    [Fact(DisplayName = "PT-06: SKU y campos obligatorios; 400 sin datos")]
    public async Task PT06_CamposObligatorios()
    {
        var admin = await LoginAdminAsync();
        using var cliente = Cliente(admin);

        var respuesta = await cliente.PostAsJsonAsync("/api/productos",
            new { sku = "", nombre = "", marcaId = 0, categoriaId = 0, unidad = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var error = await LeerErrorAsync(respuesta);
        Assert.Equal("VALIDACION", error!.Codigo);
    }

    [Fact(DisplayName = "PT-07: editar nombre conserva folios; cambiar unidad con historial se rechaza")]
    public async Task PT07_EdicionPreservaHistorial()
    {
        var admin = await LoginAdminAsync();
        var sku = SkuUnico();
        var productoId = await CrearProductoAsync(admin, sku);

        using var cliente = Cliente(admin);
        var entrada = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId,
            cantidad = 3,
            proveedor = "Distribuidora",
            referencia = "OC-1"
        });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);
        var movimiento = await entrada.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        var folio = movimiento!.Folio;

        var productos = await cliente.GetFromJsonAsync<List<ProductoJson>>($"/api/productos?texto={sku}");
        var producto = productos!.Single(p => p.Sku == sku);

        var edicion = await cliente.PutAsJsonAsync($"/api/productos/{producto.Id}", new
        {
            sku,
            nombre = "Producto renombrado",
            marcaId = producto.MarcaId,
            categoriaId = producto.CategoriaId,
            unidad = "pieza"
        });
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);

        var historial = await cliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={producto.Id}");
        Assert.Contains(historial!.Elementos, m => m.Folio == folio && m.Sku == sku && m.Cantidad == 3);

        var cambioUnidad = await cliente.PutAsJsonAsync($"/api/productos/{producto.Id}", new
        {
            sku,
            nombre = "Producto renombrado",
            marcaId = producto.MarcaId,
            categoriaId = producto.CategoriaId,
            unidad = "caja"
        });
        Assert.Equal(HttpStatusCode.Conflict, cambioUnidad.StatusCode);
        var error = await LeerErrorAsync(cambioUnidad);
        Assert.Equal("MODIFICACION_PROHIBIDA", error!.Codigo);
    }

    [Fact(DisplayName = "HU-05: marca y categoría con nombre duplicado se rechazan")]
    public async Task NombresDuplicados()
    {
        var admin = await LoginAdminAsync();
        var nombre = $"Cerveza {Guid.NewGuid():N}"[..24];

        using var cliente = Cliente(admin);
        var creacion = await cliente.PostAsJsonAsync("/api/marcas", new { nombre });
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);

        var duplicada = await cliente.PostAsJsonAsync("/api/marcas", new { nombre });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);

        var categoria = $"Categoria {Guid.NewGuid():N}"[..24];
        var creacionCat = await cliente.PostAsJsonAsync("/api/categorias", new { nombre = categoria });
        Assert.Equal(HttpStatusCode.Created, creacionCat.StatusCode);

        var duplicadaCat = await cliente.PostAsJsonAsync("/api/categorias", new { nombre = categoria });
        Assert.Equal(HttpStatusCode.Conflict, duplicadaCat.StatusCode);
    }

    [Fact(DisplayName = "HU-08: baja lógica; el producto inactivo no acepta movimientos")]
    public async Task BajaLogicaProducto()
    {
        var admin = await LoginAdminAsync();
        var sku = SkuUnico();
        var productoId = await CrearProductoAsync(admin, sku);

        using var cliente = Cliente(admin);
        var patch = await cliente.PatchAsJsonAsync($"/api/productos/{productoId}/estado",
            new { activo = false });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var entrada = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId,
            cantidad = 1,
            proveedor = "Distribuidora"
        });
        Assert.Equal(HttpStatusCode.NotFound, entrada.StatusCode);

        var productos = await cliente.GetFromJsonAsync<List<ProductoJson>>("/api/productos?activo=false");
        Assert.Contains(productos!, p => p.Id == productoId && !p.Activo);

        var reactivacion = await cliente.PatchAsJsonAsync($"/api/productos/{productoId}/estado",
            new { activo = true });
        Assert.Equal(HttpStatusCode.OK, reactivacion.StatusCode);

        var entradaPosterior = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId,
            cantidad = 1,
            proveedor = "Distribuidora"
        });
        Assert.Equal(HttpStatusCode.OK, entradaPosterior.StatusCode);
    }

    [Fact(DisplayName = "HU-07: búsqueda por texto y filtros de marca/categoría")]
    public async Task BusquedaYFiltros()
    {
        var admin = await LoginAdminAsync();
        var sku = SkuUnico();
        var productoId = await CrearProductoAsync(admin, sku);

        using var cliente = Cliente(admin);
        var porSku = await cliente.GetFromJsonAsync<List<ProductoJson>>($"/api/productos?texto={sku}");
        Assert.Contains(porSku!, p => p.Id == productoId);

        var porNombre = await cliente.GetFromJsonAsync<List<ProductoJson>>(
            "/api/productos?texto=renombrado-no-existe");
        Assert.Empty(porNombre!);

        var productos = await cliente.GetFromJsonAsync<List<ProductoJson>>($"/api/productos?texto={sku}");
        var producto = productos!.Single(p => p.Id == productoId);

        var porMarca = await cliente.GetFromJsonAsync<List<ProductoJson>>(
            $"/api/productos?marcaId={producto.MarcaId}&texto={sku}");
        Assert.Contains(porMarca!, p => p.Id == productoId);

        var porCategoria = await cliente.GetFromJsonAsync<List<ProductoJson>>(
            $"/api/productos?categoriaId={producto.CategoriaId}&texto={sku}");
        Assert.Contains(porCategoria!, p => p.Id == productoId);
    }
}
