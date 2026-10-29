using Ingrata.Api.Modules.Catalog.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Dtos;
using Ingrata.Api.Shared.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ingrata.Api.Modules.Catalog;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly ICatalogoRepositorio _catalogo;
    private readonly IBitacoraService _bitacora;

    public CategoriasController(ICatalogoRepositorio catalogo, IBitacoraService bitacora)
    {
        _catalogo = catalogo;
        _bitacora = bitacora;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<CategoriaDto>>> Listar([FromQuery] bool? activo)
    {
        var lista = await _catalogo.ListarCategoriasAsync(activo);
        return Ok(lista.Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo }).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CategoriaDto>> Alta([FromBody] CategoriaRequest solicitud)
    {
        var nombre = solicitud.Nombre.Trim();
        if (await _catalogo.ExisteNombreCategoriaAsync(nombre))
            throw ExcepcionDeNegocio.Conflicto("Ya existe una categoria con ese nombre.");

        var categoria = await _catalogo.AltaCategoriaAsync(nombre);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Alta de categoria '{categoria.Nombre}'.");

        return Created($"/api/categorias/{categoria.Id}",
            new CategoriaDto { Id = categoria.Id, Nombre = categoria.Nombre, Activo = categoria.Activo });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CategoriaDto>> Editar(int id, [FromBody] CategoriaRequest solicitud)
    {
        var categoria = await _catalogo.PorIdCategoriaAsync(id);
        if (categoria is null)
            throw ExcepcionDeNegocio.NoEncontrado("La categoria no existe.");

        var nombre = solicitud.Nombre.Trim();
        if (await _catalogo.ExisteNombreCategoriaAsync(nombre, id))
            throw ExcepcionDeNegocio.Conflicto("Ya existe una categoria con ese nombre.");

        await _catalogo.EditarCategoriaAsync(id, nombre);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Edicion de categoria '{nombre}'.");

        var actualizada = await _catalogo.PorIdCategoriaAsync(id);
        return Ok(new CategoriaDto { Id = id, Nombre = actualizada!.Nombre, Activo = actualizada.Activo });
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CategoriaDto>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest solicitud)
    {
        var categoria = await _catalogo.PorIdCategoriaAsync(id);
        if (categoria is null)
            throw ExcepcionDeNegocio.NoEncontrado("La categoria no existe.");

        await _catalogo.CambiarEstadoCategoriaAsync(id, solicitud.Activo);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"{(solicitud.Activo ? "Activacion" : "Desactivacion")} de categoria '{categoria.Nombre}'.");

        var actualizada = await _catalogo.PorIdCategoriaAsync(id);
        return Ok(new CategoriaDto { Id = id, Nombre = actualizada!.Nombre, Activo = actualizada.Activo });
    }
}
