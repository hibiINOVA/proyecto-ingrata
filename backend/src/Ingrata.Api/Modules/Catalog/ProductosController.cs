using Ingrata.Api.Modules.Catalog.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Dtos;
using Ingrata.Api.Shared.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ingrata.Api.Modules.Catalog;

[ApiController]
[Route("api/productos")]
public class ProductosController : ControllerBase
{
    private readonly ICatalogoRepositorio _catalogo;
    private readonly IBitacoraService _bitacora;

    public ProductosController(ICatalogoRepositorio catalogo, IBitacoraService bitacora)
    {
        _catalogo = catalogo;
        _bitacora = bitacora;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ProductoDto>>> Listar([FromQuery] ProductoFiltros filtros)
    {
        var lista = await _catalogo.ListarProductosAsync(filtros);
        return Ok(lista.Select(Mapear).ToList());
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ProductoDto>> PorId(int id)
    {
        var producto = await _catalogo.PorIdProductoAsync(id);
        if (producto is null)
            throw ExcepcionDeNegocio.NoEncontrado("El producto no existe.");
        return Ok(Mapear(producto));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductoDto>> Alta([FromBody] ProductoRequest solicitud)
    {
        var sku = solicitud.Sku.Trim();
        if (await _catalogo.ExisteSkuAsync(sku))
            throw ExcepcionDeNegocio.Conflicto("Ya existe un producto con ese SKU.");

        await ValidarRelacionesAsync(solicitud);

        var producto = await _catalogo.AltaProductoAsync(sku, solicitud.Nombre,
            solicitud.MarcaId, solicitud.CategoriaId, solicitud.Unidad);

        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Alta de producto '{producto.Sku}'.");

        var creado = await _catalogo.PorIdProductoAsync(producto.Id);
        return Created($"/api/productos/{producto.Id}", Mapear(creado!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductoDto>> Editar(int id, [FromBody] ProductoRequest solicitud)
    {
        var producto = await _catalogo.PorIdProductoAsync(id);
        if (producto is null)
            throw ExcepcionDeNegocio.NoEncontrado("El producto no existe.");

        var sku = solicitud.Sku.Trim();
        if (await _catalogo.ExisteSkuAsync(sku, id))
            throw ExcepcionDeNegocio.Conflicto("Ya existe un producto con ese SKU.");

        await ValidarRelacionesAsync(solicitud);

        var unidadSolicitada = solicitud.Unidad.Trim();
        if (!string.Equals(producto.Unidad, unidadSolicitada, StringComparison.OrdinalIgnoreCase) &&
            await _catalogo.ProductoTieneMovimientosAsync(id))
            throw new ExcepcionDeNegocio(CodigosError.ModificacionProhibida,
                "El producto ya tiene movimientos: no puede cambiar su unidad. Registre un producto nuevo con otro SKU.",
                409);

        await _catalogo.EditarProductoAsync(id, sku, solicitud.Nombre,
            solicitud.MarcaId, solicitud.CategoriaId, unidadSolicitada);

        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Edicion de producto '{sku}'.");

        var editado = await _catalogo.PorIdProductoAsync(id);
        return Ok(Mapear(editado!));
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductoDto>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest solicitud)
    {
        var producto = await _catalogo.PorIdProductoAsync(id);
        if (producto is null)
            throw ExcepcionDeNegocio.NoEncontrado("El producto no existe.");

        await _catalogo.CambiarEstadoProductoAsync(id, solicitud.Activo);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"{(solicitud.Activo ? "Activacion" : "Desactivacion")} de producto '{producto.Sku}'.");

        var actualizado = await _catalogo.PorIdProductoAsync(id);
        return Ok(Mapear(actualizado!));
    }

    private async Task ValidarRelacionesAsync(ProductoRequest solicitud)
    {
        var errores = new Dictionary<string, string[]>();

        if (await _catalogo.PorIdMarcaAsync(solicitud.MarcaId) is null)
            errores["marcaId"] = new[] { "La marca indicada no existe." };

        if (await _catalogo.PorIdCategoriaAsync(solicitud.CategoriaId) is null)
            errores["categoriaId"] = new[] { "La categoria indicada no existe." };

        if (errores.Count > 0)
            throw ExcepcionDeNegocio.Validacion("Los datos de referencia no son validos.", errores);
    }

    private static ProductoDto Mapear(Shared.Datos.ProductoEntity p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Nombre = p.Nombre,
        MarcaId = p.MarcaId,
        Marca = p.Marca?.Nombre ?? "",
        CategoriaId = p.CategoriaId,
        Categoria = p.Categoria?.Nombre ?? "",
        Unidad = p.Unidad,
        Activo = p.Activo,
        Existencia = p.Saldo?.Existencia ?? 0
    };
}
