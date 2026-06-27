using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Models;

namespace RentaFacil.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Inquilino> Inquilinos { get; set; }
    public DbSet<Inmueble> Inmuebles { get; set; }
    public DbSet<Unidad> Unidades { get; set; }
    public DbSet<Contrato> Contratos { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Schemas organizacionales (fijos, no por tenant) ──────────────
        // auth  → identidad/acceso (no lleva UsuarioId, ESTA tabla es el usuario)
        // renta → dominio del negocio (cada fila filtrada por UsuarioId)
        // config → catálogos globales + tabla de migraciones (__EFMigrationsHistory)
        // audit → trazabilidad (hoy vive como columnas IAuditable en renta.*)
        modelBuilder.Entity<Usuario>().ToTable("Usuarios", "auth");
        modelBuilder.Entity<Inquilino>().ToTable("Inquilinos", "renta");
        modelBuilder.Entity<Inmueble>().ToTable("Inmuebles", "renta");
        modelBuilder.Entity<Unidad>().ToTable("Unidades", "renta");
        modelBuilder.Entity<Contrato>().ToTable("Contratos", "renta");
        modelBuilder.Entity<Pago>().ToTable("Pagos", "renta");

        // Índices de UsuarioId en renta.* — SQL Server no indexa FKs ni este
        // campo automáticamente, y el WHERE UsuarioId = X corre en cada request.
        modelBuilder.Entity<Inquilino>().HasIndex(i => i.UsuarioId);
        modelBuilder.Entity<Inmueble>().HasIndex(i => i.UsuarioId);
        modelBuilder.Entity<Unidad>().HasIndex(u => u.UsuarioId);
        modelBuilder.Entity<Contrato>().HasIndex(c => c.UsuarioId);
        modelBuilder.Entity<Pago>().HasIndex(p => p.UsuarioId);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Inmueble>()
            .HasMany(i => i.Unidades)
            .WithOne(u => u.Inmueble)
            .HasForeignKey(u => u.InmuebleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Inquilino>()
            .HasMany(i => i.Contratos)
            .WithOne(c => c.Inquilino)
            .HasForeignKey(c => c.InquilinoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contrato>()
            .HasMany(c => c.Pagos)
            .WithOne(p => p.Contrato)
            .HasForeignKey(p => p.ContratoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Contrato>()
            .HasOne(c => c.Unidad)
            .WithMany()
            .HasForeignKey(c => c.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
