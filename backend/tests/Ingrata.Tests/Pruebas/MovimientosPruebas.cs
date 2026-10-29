using System.Net;
using System.Net.Http.Json;

namespace Ingrata.Tests.Pruebas;

public class MovimientosPruebas : PruebasApiBase
{
    public MovimientosPruebas(IngrataApiFactory fabrica) : base(fabrica) { }

    [Fact(DisplayName = "PT-10: entrada, entrega y merma del ERS (2+5=7, entrega 2 => 5, merma 1 => 4)")]
    public async Task PT10_EjemploErs()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        var e1 = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 2, proveedor = "Distribuidora Norte", referencia = "OC-100"
        });
        Assert.Equal(HttpStatusCode.OK, e1.StatusCode);

        var e2 = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 5, proveedor = "Distribuidora Norte", referencia = "OC-101"
        });
        Assert.Equal(HttpStatusCode.OK, e2.StatusCode);
        var r2 = await e2.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        Assert.Equal(7, r2!.ExistenciaResultante);

        var entrega = await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        {
            productoId, cantidad = 2, destino = "Barra principal"
        });
        Assert.Equal(HttpStatusCode.OK, entrega.StatusCode);
        var rEntrega = await entrega.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        Assert.Equal(5, rEntrega!.ExistenciaResultante);

        var merma = await cliente.PostAsJsonAsync("/api/movimientos/mermas", new
        {
            productoId, cantidad = 1, motivo = "Botella rota"
        });
        Assert.Equal(HttpStatusCode.OK, merma.StatusCode);
        var rMerma = await merma.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        Assert.Equal(4, rMerma!.ExistenciaResultante);
    }

    [Fact(DisplayName = "PT-11/PT-12: cargas invalidas (cantidad 0, sin proveedor/destino/motivo)")]
    public async Task CargasInvalidas()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        var cantidadCero = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 0, proveedor = "X"
        });
        Assert.Equal(HttpStatusCode.BadRequest, cantidadCero.StatusCode);

        var sinProveedor = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, sinProveedor.StatusCode);
        var errorProveedor = await LeerErrorAsync(sinProveedor);
        Assert.Equal("DATOS_INVALIDOS", errorProveedor!.Codigo);

        var sinDestino = await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        {
            productoId, cantidad = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, sinDestino.StatusCode);

        var sinMotivo = await cliente.PostAsJsonAsync("/api/movimientos/mermas", new
        {
            productoId, cantidad = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
    }

    [Fact(DisplayName = "PT-12: producto inexistente responde 404; saldo insuficiente responde 409")]
    public async Task ProductoInexistenteYSaldoInsuficiente()
    {
        var admin = await LoginAdminAsync();
        using var cliente = Cliente(admin);

        var inexistente = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId = 999999, cantidad = 1, proveedor = "X"
        });
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);

        var productoId = await CrearProductoAsync(admin);
        var sinSaldo = await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        {
            productoId, cantidad = 1, destino = "Barra"
        });
        Assert.Equal(HttpStatusCode.Conflict, sinSaldo.StatusCode);
        var error = await LeerErrorAsync(sinSaldo);
        Assert.Equal("SALDO_INSUFICIENTE", error!.Codigo);
    }

    [Fact(DisplayName = "Idempotencia: un reintento con el mismo requestId no duplica el movimiento")]
    public async Task ReintentoNoDuplica()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        var requestId = Guid.NewGuid();
        using var cliente = Cliente(admin);

        var primera = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 4, proveedor = "Distribuidora", requestId
        });
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        var r1 = await primera.Content.ReadFromJsonAsync<MovimientoResultadoJson>();

        var segunda = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 4, proveedor = "Distribuidora", requestId
        });
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var r2 = await segunda.Content.ReadFromJsonAsync<MovimientoResultadoJson>();

        Assert.Equal(r1!.Folio, r2!.Folio);
        Assert.Equal(4, r2.ExistenciaResultante);

        using var admin2 = Cliente(await LoginAdminAsync());
        var historial = await admin2.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");
        Assert.Single(historial!.Elementos);
    }

    [Fact(DisplayName = "HU-18: folios únicos y consecutivos por movimiento")]
    public async Task FoliosUnicos()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        var folios = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var respuesta = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
            {
                productoId, cantidad = 1, proveedor = "Distribuidora"
            });
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var resultado = await respuesta.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
            folios.Add(resultado!.Folio);
        }

        Assert.Equal(3, folios.Distinct().Count());
        Assert.All(folios, f => Assert.StartsWith("MOV-", f));
    }
}
