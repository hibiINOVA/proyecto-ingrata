using System.Net;
using System.Net.Http.Json;

namespace Ingrata.Tests.Pruebas;

public class ConsultasPruebas : PruebasApiBase
{
    public ConsultasPruebas(IngrataApiFactory fabrica) : base(fabrica) { }

    [Fact(DisplayName = "HU-19/HU-21: dashboard refleja existencias y totales del periodo")]
    public async Task DashboardTotales()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        { productoId, cantidad = 8, proveedor = "Distribuidora" });
        await cliente.PostAsJsonAsync("/api/movimientos/entregas", new
        { productoId, cantidad = 3, destino = "Barra" });
        await cliente.PostAsJsonAsync("/api/movimientos/mermas", new
        { productoId, cantidad = 1, motivo = "Merma" });

        var dashboard = await cliente.GetFromJsonAsync<DashboardJson>("/api/dashboard");
        Assert.NotNull(dashboard);
        Assert.Contains(dashboard!.Existencias, e => e.ProductoId == productoId && e.Existencia == 4);
        Assert.True(dashboard.TotalExistencias >= 4);
        Assert.True(dashboard.Totales.Entradas >= 8);
        Assert.True(dashboard.Totales.Entregas >= 3);
        Assert.True(dashboard.Totales.Mermas >= 1);
        Assert.Equal("America/Mexico_City", dashboard.ZonaHoraria);
        Assert.True(dashboard.AhoraLocal > default(DateTime));
    }

    [Fact(DisplayName = "HU-20: historial filtrable por fechas, producto, tipo y usuario")]
    public async Task HistorialFiltrable()
    {
        var admin = await LoginAdminAsync();
        var (almacenista, identificadorAlm) = await CrearAlmacenistaAsync(admin);
        var productoId = await CrearProductoAsync(admin);
        var otroProductoId = await CrearProductoAsync(admin);

        using var clienteAlm = Cliente(almacenista);
        await clienteAlm.PostAsJsonAsync("/api/movimientos/entradas", new
        { productoId, cantidad = 2, proveedor = "Distribuidora" });

        using var adminCliente = Cliente(admin);
        await adminCliente.PostAsJsonAsync("/api/movimientos/entradas", new
        { productoId = otroProductoId, cantidad = 1, proveedor = "Distribuidora" });

        var porProducto = await adminCliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");
        Assert.All(porProducto!.Elementos, m => Assert.Equal(productoId, m.ProductoId));

        var porTipo = await adminCliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}&tipo=1");
        Assert.All(porTipo!.Elementos, m => Assert.Equal(1, m.Tipo));

        var hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var porFecha = await adminCliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?desde={hoy}&hasta={hoy}");
        Assert.NotEmpty(porFecha!.Elementos);

        var usuarios = await adminCliente.GetFromJsonAsync<List<UsuarioJson>>("/api/usuarios");
        var alm = usuarios!.Single(u => u.Identificador == identificadorAlm);
        var porUsuario = await adminCliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?usuarioId={alm.Id}");
        Assert.All(porUsuario!.Elementos, m => Assert.Equal(alm.Id, m.UsuarioId));

        // Almacenista no puede filtrar por otros usuarios
        var denegado = await clienteAlm.GetAsync($"/api/movimientos?usuarioId={usuarios!.First(u => u.RolId == 1).Id}");
        Assert.Equal(HttpStatusCode.Forbidden, denegado.StatusCode);

        // Pero sí puede consultar su propio historial
        var propio = await clienteAlm.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?usuarioId={alm.Id}");
        Assert.NotEmpty(propio!.Elementos);
    }

    [Fact(DisplayName = "PT-21: el mismo folio consultado dos veces muestra la misma hora")]
    public async Task PT21_MismaHoraLocal()
    {
        var admin = await LoginAdminAsync();
        var productoId = await CrearProductoAsync(admin);
        using var cliente = Cliente(admin);

        var registro = await cliente.PostAsJsonAsync("/api/movimientos/entradas", new
        { productoId, cantidad = 1, proveedor = "Distribuidora" });
        var folio = (await registro.Content.ReadFromJsonAsync<MovimientoResultadoJson>())!.Folio;

        var consulta1 = await cliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");
        var consulta2 = await cliente.GetFromJsonAsync<RespuestaPaginadaJson<MovimientoJson>>(
            $"/api/movimientos?productoId={productoId}");

        var m1 = consulta1!.Elementos.Single(m => m.Folio == folio);
        var m2 = consulta2!.Elementos.Single(m => m.Folio == folio);

        Assert.Equal(m1.FechaUtc, m2.FechaUtc);

        var formato = new System.Globalization.CultureInfo("es-MX");
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        var local1 = TimeZoneInfo.ConvertTimeFromUtc(m1.FechaUtc, zona);
        var local2 = TimeZoneInfo.ConvertTimeFromUtc(m2.FechaUtc, zona);
        Assert.Equal(local1.ToString(formato), local2.ToString(formato));
    }

    [Fact(DisplayName = "PT-28/PT-38: códigos HTTP 400/401/403/404/409 según el caso")]
    public async Task CodigosHttp()
    {
        var admin = await LoginAdminAsync();

        // 401 sin token
        using var anonimo = Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/movimientos")).StatusCode);

        // 400 datos inválidos
        using var cliente = Cliente(admin);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync("/api/movimientos/mermas",
                new { productoId = 1, cantidad = 0, motivo = "x" })).StatusCode);

        // 403 con rol insuficiente
        var (almacenista, _) = await CrearAlmacenistaAsync(admin);
        using var clienteAlm = Cliente(almacenista);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await clienteAlm.PostAsJsonAsync("/api/usuarios", new { })).StatusCode);

        // 404 inexistente
        Assert.Equal(HttpStatusCode.NotFound,
            (await cliente.GetAsync("/api/productos/99999999")).StatusCode);

        // 409 conflicto de negocio
        var productoId = await CrearProductoAsync(admin);
        Assert.Equal(HttpStatusCode.Conflict,
            (await cliente.PostAsJsonAsync("/api/movimientos/entregas",
                new { productoId, cantidad = 5, destino = "Barra" })).StatusCode);
    }

    [Fact(DisplayName = "HU-XX: bitácora registra login y gestión; solo Admin la consulta")]
    public async Task BitacoraRegistraEventos()
    {
        var admin = await LoginAdminAsync();
        var nombreMarca = $"Audit {Guid.NewGuid():N}"[..20];

        using var cliente = Cliente(admin);
        await cliente.PostAsJsonAsync("/api/marcas", new { nombre = nombreMarca });

        var bitacora = await cliente.GetFromJsonAsync<RespuestaPaginadaJson<BitacoraJson>>(
            "/api/bitacora?pagina=1");
        Assert.NotEmpty(bitacora!.Elementos);
        Assert.Contains(bitacora.Elementos, b => b.Evento == "login");
        Assert.Contains(bitacora.Elementos, b => b.Evento == "catalogo" &&
            b.Detalle != null && b.Detalle.Contains(nombreMarca));
    }
}

public record BitacoraJson(long Id, DateTime FechaUtc, int? UsuarioId, string? Usuario,
    string Evento, string? Detalle);
