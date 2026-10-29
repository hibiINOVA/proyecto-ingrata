using Ingrata.Api.Modules.Auth.Dtos;
using Ingrata.Api.Shared.Bitacora;
using Ingrata.Api.Shared.Claims;
using Ingrata.Api.Shared.Datos;
using Ingrata.Api.Shared.Dtos;
using Ingrata.Api.Shared.Errores;
using Ingrata.Api.Shared.Validacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Auth;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IngrataDbContext _db;
    private readonly IBitacoraService _bitacora;

    public UsuariosController(IUsuarioRepositorio usuarios, IngrataDbContext db, IBitacoraService bitacora)
    {
        _usuarios = usuarios;
        _db = db;
        _bitacora = bitacora;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar()
    {
        var lista = await _usuarios.ListarAsync();
        return Ok(lista.Select(Mapear).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> PorId(int id)
    {
        var usuario = await _usuarios.PorIdAsync(id);
        if (usuario is null)
            throw ExcepcionDeNegocio.NoEncontrado("El usuario no existe.");
        return Ok(Mapear(usuario));
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Alta([FromBody] CrearUsuarioRequest solicitud)
    {
        await ValidarRolAsync(solicitud.RolId);

        if (await _usuarios.ExisteIdentificadorAsync(solicitud.Identificador.Trim()))
            throw ExcepcionDeNegocio.Conflicto("Ya existe un usuario con ese identificador.");

        var usuario = await _usuarios.AltaAsync(solicitud.Identificador, solicitud.NombreCompleto,
            PasswordHasher.Hash(solicitud.Password), solicitud.RolId);

        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "usuarios",
            $"Alta de usuario '{usuario.Identificador}'.");

        return CreatedAtAction(nameof(PorId), new { id = usuario.Id }, Mapear(usuario));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Editar(int id, [FromBody] EditarUsuarioRequest solicitud)
    {
        var usuario = await _usuarios.PorIdAsync(id);
        if (usuario is null)
            throw ExcepcionDeNegocio.NoEncontrado("El usuario no existe.");

        await ValidarRolAsync(solicitud.RolId);

        await _usuarios.EditarAsync(id, solicitud.NombreCompleto, solicitud.RolId,
            string.IsNullOrWhiteSpace(solicitud.Password) ? null : PasswordHasher.Hash(solicitud.Password));

        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "usuarios",
            $"Edicion de usuario '{usuario.Identificador}'.");

        var actualizado = await _usuarios.PorIdAsync(id);
        return Ok(Mapear(actualizado!));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<UsuarioDto>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest solicitud)
    {
        var usuario = await _usuarios.PorIdAsync(id);
        if (usuario is null)
            throw ExcepcionDeNegocio.NoEncontrado("El usuario no existe.");

        var yoId = HttpContext.User.ObtenerUsuarioId();

        if (!solicitud.Activo && id == yoId)
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "No puede desactivar su propia cuenta.", 409);

        if (!solicitud.Activo && usuario.RolId == 1 && usuario.Activo &&
            await _usuarios.ContarAdministradoresActivosAsync() <= 1)
            throw new ExcepcionDeNegocio(CodigosError.DatosInvalidos,
                "No es posible desactivar al ultimo administrador activo.", 409);

        await _usuarios.CambiarEstadoAsync(id, solicitud.Activo);

        await _bitacora.RegistrarDesdeContextoAsync(HttpContext, "usuarios",
            $"{(solicitud.Activo ? "Activacion" : "Desactivacion")} de usuario '{usuario.Identificador}'.");

        var actualizado = await _usuarios.PorIdAsync(id);
        return Ok(Mapear(actualizado!));
    }

    private async Task ValidarRolAsync(byte rolId)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == rolId))
            throw ExcepcionDeNegocio.Validacion("El rol indicado no existe.",
                new Dictionary<string, string[]> { ["rolId"] = new[] { "El rol indicado no existe." } });
    }

    private static UsuarioDto Mapear(UsuarioEntity u) => new()
    {
        Id = u.Id,
        Identificador = u.Identificador,
        NombreCompleto = u.NombreCompleto,
        RolId = u.RolId,
        Rol = u.Rol?.Nombre ?? "",
        Activo = u.Activo,
        CreadoUtc = DateTime.SpecifyKind(u.CreadoUtc, DateTimeKind.Utc)
    };
}
