using Ingrata.Api.Modules.Catalog.Dtos;
using Ingrata.Api.Shared.Datos;
using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Modules.Catalog;

public interface ICatalogoRepositorio
{
    Task<List<MarcaEntity>> ListarMarcasAsync(bool? activo);
    Task<MarcaEntity?> PorIdMarcaAsync(int id);
    Task<bool> ExisteNombreMarcaAsync(string nombre, int? excluirId = null);
    Task<MarcaEntity> AltaMarcaAsync(string nombre);
    Task EditarMarcaAsync(int id, string nombre);
    Task CambiarEstadoMarcaAsync(int id, bool activo);

    Task<List<CategoriaEntity>> ListarCategoriasAsync(bool? activo);
    Task<CategoriaEntity?> PorIdCategoriaAsync(int id);
    Task<bool> ExisteNombreCategoriaAsync(string nombre, int? excluirId = null);
    Task<CategoriaEntity> AltaCategoriaAsync(string nombre);
    Task EditarCategoriaAsync(int id, string nombre);
    Task CambiarEstadoCategoriaAsync(int id, bool activo);

    Task<List<ProductoEntity>> ListarProductosAsync(ProductoFiltros filtros);
    Task<ProductoEntity?> PorIdProductoAsync(int id);
    Task<bool> ExisteSkuAsync(string sku, int? excluirId = null);
    Task<bool> ProductoTieneMovimientosAsync(int productoId);
    Task<ProductoEntity> AltaProductoAsync(string sku, string nombre, int marcaId, int categoriaId, string unidad);
    Task EditarProductoAsync(int id, string sku, string nombre, int marcaId, int categoriaId, string unidad);
    Task CambiarEstadoProductoAsync(int id, bool activo);
}

public sealed class CatalogoRepositorio : ICatalogoRepositorio
{
    private readonly IngrataDbContext _db;

    public CatalogoRepositorio(IngrataDbContext db) => _db = db;

    public Task<List<MarcaEntity>> ListarMarcasAsync(bool? activo)
    {
        var consulta = _db.Marcas.AsNoTracking().AsQueryable();
        if (activo.HasValue)
            consulta = consulta.Where(m => m.Activo == activo.Value);
        return consulta.OrderBy(m => m.Nombre).ToListAsync();
    }

    public Task<MarcaEntity?> PorIdMarcaAsync(int id) =>
        _db.Marcas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    public Task<bool> ExisteNombreMarcaAsync(string nombre, int? excluirId = null) =>
        _db.Marcas.AnyAsync(m => m.Nombre == nombre &&
                                  (excluirId == null || m.Id != excluirId));

    public async Task<MarcaEntity> AltaMarcaAsync(string nombre)
    {
        var marca = new MarcaEntity { Nombre = nombre.Trim(), Activo = true };
        _db.Marcas.Add(marca);
        await _db.SaveChangesAsync();
        return marca;
    }

    public async Task EditarMarcaAsync(int id, string nombre)
    {
        var marca = await _db.Marcas.FindAsync(id) ?? throw new KeyNotFoundException();
        marca.Nombre = nombre.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task CambiarEstadoMarcaAsync(int id, bool activo)
    {
        var marca = await _db.Marcas.FindAsync(id) ?? throw new KeyNotFoundException();
        marca.Activo = activo;
        await _db.SaveChangesAsync();
    }

    public Task<List<CategoriaEntity>> ListarCategoriasAsync(bool? activo)
    {
        var consulta = _db.Categorias.AsNoTracking().AsQueryable();
        if (activo.HasValue)
            consulta = consulta.Where(c => c.Activo == activo.Value);
        return consulta.OrderBy(c => c.Nombre).ToListAsync();
    }

    public Task<CategoriaEntity?> PorIdCategoriaAsync(int id) =>
        _db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExisteNombreCategoriaAsync(string nombre, int? excluirId = null) =>
        _db.Categorias.AnyAsync(c => c.Nombre == nombre &&
                                      (excluirId == null || c.Id != excluirId));

    public async Task<CategoriaEntity> AltaCategoriaAsync(string nombre)
    {
        var categoria = new CategoriaEntity { Nombre = nombre.Trim(), Activo = true };
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();
        return categoria;
    }

    public async Task EditarCategoriaAsync(int id, string nombre)
    {
        var categoria = await _db.Categorias.FindAsync(id) ?? throw new KeyNotFoundException();
        categoria.Nombre = nombre.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task CambiarEstadoCategoriaAsync(int id, bool activo)
    {
        var categoria = await _db.Categorias.FindAsync(id) ?? throw new KeyNotFoundException();
        categoria.Activo = activo;
        await _db.SaveChangesAsync();
    }

    public Task<List<ProductoEntity>> ListarProductosAsync(ProductoFiltros filtros)
    {
        var consulta = _db.Productos.AsNoTracking()
            .Include(p => p.Marca)
            .Include(p => p.Categoria)
            .Include(p => p.Saldo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtros.Texto))
        {
            var texto = filtros.Texto.Trim();
            consulta = consulta.Where(p =>
                p.Sku.Contains(texto) || p.Nombre.Contains(texto));
        }

        if (filtros.MarcaId.HasValue)
            consulta = consulta.Where(p => p.MarcaId == filtros.MarcaId.Value);

        if (filtros.CategoriaId.HasValue)
            consulta = consulta.Where(p => p.CategoriaId == filtros.CategoriaId.Value);

        if (filtros.Activo.HasValue)
            consulta = consulta.Where(p => p.Activo == filtros.Activo.Value);

        return consulta.OrderBy(p => p.Nombre).ToListAsync();
    }

    public Task<ProductoEntity?> PorIdProductoAsync(int id) =>
        _db.Productos.AsNoTracking()
            .Include(p => p.Marca)
            .Include(p => p.Categoria)
            .Include(p => p.Saldo)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExisteSkuAsync(string sku, int? excluirId = null) =>
        _db.Productos.AnyAsync(p => p.Sku == sku &&
                                     (excluirId == null || p.Id != excluirId));

    public Task<bool> ProductoTieneMovimientosAsync(int productoId) =>
        _db.Movimientos.AnyAsync(m => m.ProductoId == productoId);

    public async Task<ProductoEntity> AltaProductoAsync(string sku, string nombre,
        int marcaId, int categoriaId, string unidad)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        var producto = new ProductoEntity
        {
            Sku = sku.Trim(),
            Nombre = nombre.Trim(),
            MarcaId = marcaId,
            CategoriaId = categoriaId,
            Unidad = unidad.Trim(),
            Activo = true
        };
        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        _db.SaldosProducto.Add(new SaldoProductoEntity { ProductoId = producto.Id, Existencia = 0 });
        await _db.SaveChangesAsync();

        await transaccion.CommitAsync();
        return producto;
    }

    public async Task EditarProductoAsync(int id, string sku, string nombre,
        int marcaId, int categoriaId, string unidad)
    {
        var producto = await _db.Productos.FindAsync(id) ?? throw new KeyNotFoundException();
        producto.Sku = sku.Trim();
        producto.Nombre = nombre.Trim();
        producto.MarcaId = marcaId;
        producto.CategoriaId = categoriaId;
        producto.Unidad = unidad.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task CambiarEstadoProductoAsync(int id, bool activo)
    {
        var producto = await _db.Productos.FindAsync(id) ?? throw new KeyNotFoundException();
        producto.Activo = activo;
        await _db.SaveChangesAsync();
    }
}
