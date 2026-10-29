using Microsoft.EntityFrameworkCore;

namespace Ingrata.Api.Shared.Datos;

public class IngrataDbContext : DbContext
{
    public IngrataDbContext(DbContextOptions<IngrataDbContext> opciones) : base(opciones) { }

    public DbSet<RolEntity> Roles => Set<RolEntity>();
    public DbSet<UsuarioEntity> Usuarios => Set<UsuarioEntity>();
    public DbSet<MarcaEntity> Marcas => Set<MarcaEntity>();
    public DbSet<CategoriaEntity> Categorias => Set<CategoriaEntity>();
    public DbSet<ProductoEntity> Productos => Set<ProductoEntity>();
    public DbSet<SaldoProductoEntity> SaldosProducto => Set<SaldoProductoEntity>();
    public DbSet<MovimientoEntity> Movimientos => Set<MovimientoEntity>();
    public DbSet<SesionRevocadaEntity> SesionesRevocadas => Set<SesionRevocadaEntity>();
    public DbSet<BitacoraEntity> Bitacora => Set<BitacoraEntity>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<RolEntity>(e =>
        {
            e.ToTable("Rol");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        mb.Entity<UsuarioEntity>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Identificador).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Identificador).IsUnique();
            e.Property(x => x.NombreCompleto).HasMaxLength(120).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(400).IsRequired();
            e.Property(x => x.RolId);
            e.Property(x => x.Activo);
            e.Property(x => x.CreadoUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId);
        });

        mb.Entity<MarcaEntity>(e =>
        {
            e.ToTable("Marca");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.Activo).HasDefaultValue(true);
        });

        mb.Entity<CategoriaEntity>(e =>
        {
            e.ToTable("Categoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.Activo).HasDefaultValue(true);
        });

        mb.Entity<ProductoEntity>(e =>
        {
            e.ToTable("Producto");
            e.HasKey(x => x.Id);
            e.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Unidad).HasMaxLength(20).IsRequired();
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.HasOne(x => x.Marca).WithMany().HasForeignKey(x => x.MarcaId);
            e.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId);
            e.HasOne(x => x.Saldo).WithOne(x => x.Producto)
                .HasForeignKey<SaldoProductoEntity>(x => x.ProductoId);
        });

        mb.Entity<SaldoProductoEntity>(e =>
        {
            e.ToTable("SaldoProducto");
            e.HasKey(x => x.ProductoId);
            e.Property(x => x.Existencia).HasDefaultValue(0);
            e.ToTable(t => t.HasCheckConstraint("CK_Saldo_NoNegativo", "Existencia >= 0"));
        });

        mb.Entity<MovimientoEntity>(e =>
        {
            e.ToTable("Movimiento");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Folio).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Folio).IsUnique();
            e.Property(x => x.Tipo);
            e.Property(x => x.SkuSnapshot).HasMaxLength(40).IsRequired();
            e.Property(x => x.UnidadSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.Cantidad);
            e.Property(x => x.FechaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.Proveedor).HasMaxLength(120);
            e.Property(x => x.Referencia).HasMaxLength(120);
            e.Property(x => x.Destino).HasMaxLength(120);
            e.Property(x => x.Motivo).HasMaxLength(300);
            e.HasIndex(x => x.MovimientoOriginalId).IsUnique().HasFilter("[MovimientoOriginalId] IS NOT NULL");
            e.HasIndex(x => x.RequestId).IsUnique().HasFilter("[RequestId] IS NOT NULL");
            e.HasIndex(x => new { x.ProductoId, x.FechaUtc });
            e.HasIndex(x => new { x.Tipo, x.FechaUtc });
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.MovimientoOriginal).WithMany()
                .HasForeignKey(x => x.MovimientoOriginalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<SesionRevocadaEntity>(e =>
        {
            e.ToTable("SesionRevocada");
            e.HasKey(x => x.Jti);
            e.Property(x => x.Jti).ValueGeneratedNever();
            e.Property(x => x.ExpiraUtc);
        });

        mb.Entity<BitacoraEntity>(e =>
        {
            e.ToTable("Bitacora");
            e.HasKey(x => x.Id);
            e.Property(x => x.FechaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.Evento).HasMaxLength(60).IsRequired();
            e.Property(x => x.Detalle).HasMaxLength(400);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });
    }
}
