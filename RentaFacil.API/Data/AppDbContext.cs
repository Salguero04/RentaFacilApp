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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
