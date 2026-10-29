using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Ingrata.Tests.Pruebas;

public class IntegridadPruebas : PruebasApiBase
{
    public IntegridadPruebas(IngrataApiFactory fabrica) : base(fabrica) { }

    [Fact(DisplayName = "PT-16: anular entrega repone saldo; segunda anulación 409; anular entrada consumida 409")]
    public async Task PT16_Anulaciones()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        var entrada = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 5, proveedor = "Distribuidora"
        });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);
        var rEntrada = await entrada.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        var idEntrada = rEntrada!.Id!.Value;

        var entrega = await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        {
            productoId, cantidad = 2, destino = "Barra principal"
        });
        var rEntrega = await entrega.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        var idEntrega = rEntrega!.Id!.Value;
        Assert.Equal(3, rEntrega.ExistenciaResultante);

        var anulacion = await cliente.PostAsJsonAsync($"/api/movimientos/{idEntrega}/anulacion",
            new { motivo = "Entrega registrada por error" });
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        var rAnulacion = await anulacion.Content.ReadFromJsonAsync<MovimientoResultadoJson>();
        Assert.Equal(5, rAnulacion!.ExistenciaResultante);

        var segunda = await cliente.PostAsJsonAsync($"/api/movimientos/{idEntrega}/anulacion",
            new { motivo = "Otra vez" });
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        var errorSegunda = await LeerErrorAsync(segunda);
        Assert.Equal("YA_ANULADO", errorSegunda!.Codigo);

        // Consumir la entrada (dejar saldo en 0) y luego intentar anularla
        var consumo = await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        {
            productoId, cantidad = 5, destino = "Barra principal"
        });
        Assert.Equal(HttpStatusCode.OK, consumo.StatusCode);

        var anularEntrada = await cliente.PostAsJsonAsync($"/api/movimientos/{idEntrada}/anulacion",
            new { motivo = "Intento de anular entrada consumida" });
        Assert.Equal(HttpStatusCode.Conflict, anularEntrada.StatusCode);
        var errorEntrada = await LeerErrorAsync(anularEntrada);
        Assert.Equal("SALDO_INSUFICIENTE", errorEntrada!.Codigo);
    }

    [Fact(DisplayName = "PT-13: dos entregas simultáneas por la última unidad confirman exactamente una")]
    public async Task PT13_ConcurrenciaUltimaUnidad()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);

        using var preparacion = Cliente(admin);
        var entrada = await preparacion.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 1, proveedor = "Distribuidora"
        });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);

        using var clienteA = Cliente(admin);
        using var clienteB = Cliente(admin);

        var solicitud = new
        {
            productoId,
            cantidad = 1,
            destino = "Barra principal"
        };

        var tareaA = clienteA.PostAsJsonAsync("/api/movimientos/entregas", solicitud);
        var tareaB = clienteB.PostAsJsonAsync("/api/movimientos/entregas", solicitud);
        var respuestas = await Task.WhenAll(tareaA, tareaB);

        var exitos = respuestas.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictos = respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, exitos);
        Assert.Equal(1, conflictos);

        Assert.Equal(0, await ExistenciaAsync(productoId));
    }

    [Fact(DisplayName = "PT-14: falla después del UPDATE de saldo provoca rollback completo")]
    public async Task PT14_RollbackTrasUpdateSaldo()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 3, proveedor = "Distribuidora"
        });
        var saldoAntes = await ExistenciaAsync(productoId);
        Assert.Equal(3, saldoAntes);

        // Tipo 2 sin destino: el SP actualiza el saldo y luego el INSERT viola CK_Mov_Datos,
        // provocando el rollback de toda la transacción (XACT_ABORT ON).
        await using var conexion = new SqlConnection(Fabrica.CadenaConexionIngrata);
        await conexion.OpenAsync();

        var excepcion = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            var parametros = new DynamicParameters();
            parametros.Add("@Tipo", (byte)2);
            parametros.Add("@ProductoId", productoId);
            parametros.Add("@Cantidad", 1);
            parametros.Add("@UsuarioId", 1);
            parametros.Add("@Destino", (string?)null);
            parametros.Add("@Folio", direction: System.Data.ParameterDirection.Output, size: 20);
            await conexion.ExecuteAsync("dbo.sp_RegistrarMovimiento", parametros,
                commandType: System.Data.CommandType.StoredProcedure);
        });

        Assert.NotNull(excepcion);

        var saldoDespues = await ExistenciaAsync(productoId);
        Assert.Equal(3, saldoDespues);

        await using var verificacion = new SqlConnection(Fabrica.CadenaConexionIngrata);
        await verificacion.OpenAsync();
        var cantidadMovimientos = await verificacion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Movimiento WHERE ProductoId = @ProductoId",
            new { ProductoId = productoId });
        Assert.Equal(1, cantidadMovimientos);
    }

    [Fact(DisplayName = "PT-15: UPDATE y DELETE directos sobre Movimiento y Bitacora se rechazan")]
    public async Task PT15_Inmutabilidad()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);
        await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 1, proveedor = "Distribuidora"
        });

        await using var conexion = new SqlConnection(Fabrica.CadenaConexionIngrata);
        await conexion.OpenAsync();

        await Assert.ThrowsAsync<SqlException>(() =>
            conexion.ExecuteAsync("UPDATE dbo.Movimiento SET Cantidad = 999 WHERE ProductoId = @ProductoId",
                new { ProductoId = productoId }));

        await Assert.ThrowsAsync<SqlException>(() =>
            conexion.ExecuteAsync("DELETE FROM dbo.Movimiento WHERE ProductoId = @ProductoId",
                new { ProductoId = productoId }));

        await Assert.ThrowsAsync<SqlException>(() =>
            conexion.ExecuteAsync("UPDATE dbo.Bitacora SET Evento = 'x' WHERE Id > 0"));

        await Assert.ThrowsAsync<SqlException>(() =>
            conexion.ExecuteAsync("DELETE FROM dbo.Bitacora WHERE Id > 0"));
    }

    [Fact(DisplayName = "PT-17: SaldoProducto.Existencia = suma algebraica del historial")]
    public async Task PT17_SaldoConsistenteConHistorial()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        { productoId, cantidad = 10, proveedor = "Distribuidora" });
        await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        { productoId, cantidad = 3, destino = "Barra" });
        await cliente.PostAsJsonAsync("/api/movimientos/mermas", new
        { productoId, cantidad = 2, motivo = "Merma de linea" });

        var historial = await cliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");
        Assert.Equal(3, historial!.Total);

        // Cálculo directo desde la BD: Δ = +entrada, −entrega/merma, −Δ(original) en reversión
        await using var conexion = new SqlConnection(Fabrica.CadenaConexionIngrata);
        await conexion.OpenAsync();

        var sumaBd = await conexion.ExecuteScalarAsync<int>(@"
            SELECT ISNULL(SUM(CASE
                WHEN m.Tipo = 1 THEN m.Cantidad
                WHEN m.Tipo IN (2,3) THEN -m.Cantidad
                WHEN m.Tipo = 4 AND o.Tipo = 1 THEN -m.Cantidad
                WHEN m.Tipo = 4 AND o.Tipo IN (2,3) THEN m.Cantidad
                ELSE 0 END), 0)
            FROM dbo.Movimiento m
            LEFT JOIN dbo.Movimiento o ON o.Id = m.MovimientoOriginalId
            WHERE m.ProductoId = @ProductoId",
            new { ProductoId = productoId });

        var saldo = await conexion.ExecuteScalarAsync<int>(
            "SELECT Existencia FROM dbo.SaldoProducto WHERE ProductoId = @ProductoId",
            new { ProductoId = productoId });

        Assert.Equal(sumaBd, saldo);
        Assert.Equal(5, saldo);
    }

    [Fact(DisplayName = "PT-18: desactivar usuario y producto conserva autor, SKU y unidad en el historial")]
    public async Task PT18_HistorialConservaSnapshots()
    {
        var admin = await LoginAdminAsync();
        var (almacenista, identificador) = await CrearAlmacenistaAsync(admin);
        var sku = SkuUnico();
        var productoId = await CrearProductoAsync(admin, sku, unidad: "botella");

        using var almacenistaCliente = Cliente(almacenista);
        var entrada = await almacenistaCliente.PostAsJsonAsync("/api/movimientos/entradas", new
        {
            productoId, cantidad = 6, proveedor = "Distribuidora"
        });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);

        using var adminCliente = Cliente(admin);
        await adminCliente.PatchAsJsonAsync($"/api/productos/{productoId}/estado", new { activo = false });

        var usuarios = await adminCliente.GetFromJsonAsync<List<UsuarioJson>>("/api/usuarios");
        var usuario = usuarios!.Single(u => u.Identificador == identificador);
        await adminCliente.PatchAsJsonAsync($"/api/usuarios/{usuario.Id}/estado", new { activo = false });

        var historial = await adminCliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");

        var movimiento = historial!.Elementos.Single();
        Assert.Equal(sku, movimiento.Sku);
        Assert.Equal("botella", movimiento.Unidad);
        Assert.Equal(usuario.Id, movimiento.UsuarioId);
        Assert.Equal("Almacenista de pruebas", movimiento.UsuarioNombre);
    }
}
