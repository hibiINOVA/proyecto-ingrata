using Ingrata.Api.Shared.Datos;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Auth;

public interface IUsuarioRepositorio
{
    Task<UsuarioEntity?> PorIdentificadorAsync(string identificador);
    Task<UsuarioEntity?> PorIdAsync(int id, bool incluirInactivo = true);
    Task<List<UsuarioEntity>> ListarAsync();
    Task<bool> ExisteIdentificadorAsync(string identificador, int? excluirId = null);
    Task<int> ContarAdministradoresActivosAsync();
    Task<UsuarioEntity> AltaAsync(string identificador, string nombreCompleto, string passwordHash, byte rolId);
    Task EditarAsync(int id, string nombreCompleto, byte rolId, string? passwordHash);
    Task CambiarEstadoAsync(int id, bool activo);
    Task<bool> SesionRevocadaAsync(Guid jti);
    Task RevocarSesionAsync(Guid jti, DateTime expiraUtc);
}

public sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly IngrataDbContext _db;

    public UsuarioRepositorio(IngrataDbContext db) => _db = db;

    public Task<UsuarioEntity?> PorIdentificadorAsync(string identificador) =>
        _db.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Identificador == identificador);

    public Task<UsuarioEntity?> PorIdAsync(int id, bool incluirInactivo = true)
    {
        var consulta = _db.Usuarios.Include(u => u.Rol).Where(u => u.Id == id);
        if (!incluirInactivo)
            consulta = consulta.Where(u => u.Activo);
        return consulta.FirstOrDefaultAsync();
    }

    public Task<List<UsuarioEntity>> ListarAsync() =>
        _db.Usuarios.Include(u => u.Rol).OrderBy(u => u.Identificador).ToListAsync();

    public Task<bool> ExisteIdentificadorAsync(string identificador, int? excluirId = null) =>
        _db.Usuarios.AnyAsync(u => u.Identificador == identificador &&
                                    (excluirId == null || u.Id != excluirId));

    public Task<int> ContarAdministradoresActivosAsync() =>
        _db.Usuarios.CountAsync(u => u.RolId == 1 && u.Activo);

    public async Task<UsuarioEntity> AltaAsync(string identificador, string nombreCompleto,
        string passwordHash, byte rolId)
    {
        var usuario = new UsuarioEntity
        {
            Identificador = identificador.Trim(),
            NombreCompleto = nombreCompleto.Trim(),
            PasswordHash = passwordHash,
            RolId = rolId,
            Activo = true,
            CreadoUtc = DateTime.UtcNow
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }

    public async Task EditarAsync(int id, string nombreCompleto, byte rolId, string? passwordHash)
    {
        var usuario = await _db.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException();
        usuario.NombreCompleto = nombreCompleto.Trim();
        usuario.RolId = rolId;
        if (!string.IsNullOrWhiteSpace(passwordHash))
            usuario.PasswordHash = passwordHash;
        await _db.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        var usuario = await _db.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException();
        usuario.Activo = activo;
        await _db.SaveChangesAsync();
    }

    public Task<bool> SesionRevocadaAsync(Guid jti) =>
        _db.SesionesRevocadas.AnyAsync(s => s.Jti == jti);

    public async Task RevocarSesionAsync(Guid jti, DateTime expiraUtc)
    {
        var expiradas = await _db.SesionesRevocadas
            .Where(s => s.ExpiraUtc < DateTime.UtcNow)
            .ToListAsync();
        if (expiradas.Count > 0)
            _db.SesionesRevocadas.RemoveRange(expiradas);

        if (!await _db.SesionesRevocadas.AnyAsync(s => s.Jti == jti))
            _db.SesionesRevocadas.Add(new SesionRevocadaEntity { Jti = jti, ExpiraUtc = expiraUtc });

        await _db.SaveChangesAsync();
    }
}
