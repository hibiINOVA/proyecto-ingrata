using Ingrata.Api.Modules.Catalog.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Dtos;
using Ingrata.Api.Shared.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ingrata.Api.Modules.Catalog;

[ApiController]
[Route("api/marcas")]
public class MarcasController : ControllerBase
{
    private readonly ICatalogoRepositorio _catalogo;
    private readonly IBitacoraService _bitacora;

    public MarcasController(ICatalogoRepositorio catalogo, IBitacoraService bitacora)
    {
        _catalogo = catalogo;
        _bitacora = bitacora;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<MarcaDto>>> Listar([FromQuery] bool? activo)
    {
        var lista = await _catalogo.ListarMarcasAsync(activo);
        return Ok(lista.Select(m => new MarcaDto { Id = m.Id, Nombre = m.Nombre, Activo = m.Activo }).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<MarcaDto>> Alta([FromBody] MarcaRequest solicitud)
    {
        var nombre = solicitud.Nombre.Trim();
        if (await _catalogo.ExisteNombreMarcaAsync(nombre))
            throw ExcepcionDeNegocio.Conflicto("Ya existe una marca con ese nombre.");

        var marca = await _catalogo.AltaMarcaAsync(nombre);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Alta de marca '{marca.Nombre}'.");

        return Created($"/api/marcas/{marca.Id}",
            new MarcaDto { Id = marca.Id, Nombre = marca.Nombre, Activo = marca.Activo });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<MarcaDto>> Editar(int id, [FromBody] MarcaRequest solicitud)
    {
        var marca = await _catalogo.PorIdMarcaAsync(id);
        if (marca is null)
            throw ExcepcionDeNegocio.NoEncontrado("La marca no existe.");

        var nombre = solicitud.Nombre.Trim();
        if (await _catalogo.ExisteNombreMarcaAsync(nombre, id))
            throw ExcepcionDeNegocio.Conflicto("Ya existe una marca con ese nombre.");

        await _catalogo.EditarMarcaAsync(id, nombre);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"Edicion de marca '{nombre}'.");

        var actualizada = await _catalogo.PorIdMarcaAsync(id);
        return Ok(new MarcaDto { Id = id, Nombre = actualizada!.Nombre, Activo = actualizada.Activo });
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<MarcaDto>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest solicitud)
    {
        var marca = await _catalogo.PorIdMarcaAsync(id);
        if (marca is null)
            throw ExcepcionDeNegocio.NoEncontrado("La marca no existe.");

        await _catalogo.CambiarEstadoMarcaAsync(id, solicitud.Activo);
        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "catalogo",
            $"{(solicitud.Activo ? "Activacion" : "Desactivacion")} de marca '{marca.Nombre}'.");

        var actualizada = await _catalogo.PorIdMarcaAsync(id);
        return Ok(new MarcaDto { Id = id, Nombre = actualizada!.Nombre, Activo = actualizada.Activo });
    }
}
