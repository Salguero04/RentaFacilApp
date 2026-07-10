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
    public DbSet<Recordatorio> Recordatorios { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Medidor> Medidores { get; set; }
    public DbSet<MedidorInquilino> MedidoresInquilino { get; set; }
    public DbSet<FacturaMedidor> FacturasMedidor { get; set; }
    public DbSet<DetalleServicioPago> DetallesServicioPago { get; set; }
    public DbSet<NotificacionPendiente> NotificacionesPendientes { get; set; }
    public DbSet<CodigoVinculacion> CodigosVinculacion { get; set; }
    public DbSet<ReportePago> ReportesPago { get; set; }

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
        modelBuilder.Entity<Recordatorio>().ToTable("Recordatorios", "renta");
        modelBuilder.Entity<Medidor>().ToTable("Medidores", "renta");
        modelBuilder.Entity<MedidorInquilino>().ToTable("MedidoresInquilino", "renta");
        modelBuilder.Entity<FacturaMedidor>().ToTable("FacturasMedidor", "renta");
        modelBuilder.Entity<DetalleServicioPago>().ToTable("DetallesServicioPago", "renta");
        modelBuilder.Entity<NotificacionPendiente>().ToTable("NotificacionesPendientes", "renta");
        modelBuilder.Entity<CodigoVinculacion>().ToTable("CodigosVinculacion", "renta");
        modelBuilder.Entity<ReportePago>().ToTable("ReportesPago", "renta");

        // Índices de UsuarioId en renta.* — SQL Server no indexa FKs ni este
        // campo automáticamente, y el WHERE UsuarioId = X corre en cada request.
        modelBuilder.Entity<Inquilino>().HasIndex(i => i.UsuarioId);
        modelBuilder.Entity<Inmueble>().HasIndex(i => i.UsuarioId);
        modelBuilder.Entity<Unidad>().HasIndex(u => u.UsuarioId);
        modelBuilder.Entity<Contrato>().HasIndex(c => c.UsuarioId);
        modelBuilder.Entity<Pago>().HasIndex(p => p.UsuarioId);
        modelBuilder.Entity<Recordatorio>().HasIndex(r => r.UsuarioId);
        modelBuilder.Entity<Medidor>().HasIndex(m => m.UsuarioId);
        modelBuilder.Entity<MedidorInquilino>().HasIndex(mi => mi.UsuarioId);
        modelBuilder.Entity<FacturaMedidor>().HasIndex(f => f.UsuarioId);
        modelBuilder.Entity<DetalleServicioPago>().HasIndex(d => d.UsuarioId);
        modelBuilder.Entity<NotificacionPendiente>().HasIndex(n => n.UsuarioId);
        modelBuilder.Entity<CodigoVinculacion>().HasIndex(c => c.UsuarioId);
        modelBuilder.Entity<ReportePago>().HasIndex(r => r.UsuarioId);
        modelBuilder.Entity<ReportePago>().HasIndex(r => r.CuentaInquilinoId);

        // Cuenta de acceso del inquilino (auth.Usuarios) — no único: hoy 1:1 en la práctica,
        // pero no hay una restricción de negocio que lo exija a nivel de BD.
        modelBuilder.Entity<Inquilino>().HasIndex(i => i.UsuarioCuentaId);

        // Código de vinculación QR — un solo uso, se busca por Codigo al canjear.
        modelBuilder.Entity<CodigoVinculacion>()
            .HasIndex(c => c.Codigo)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("[GoogleId] IS NOT NULL");

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email);

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

        modelBuilder.Entity<Inquilino>()
            .HasMany<Recordatorio>()
            .WithOne(r => r.Inquilino)
            .HasForeignKey(r => r.InquilinoId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Medidores de un inmueble — cascada al borrar el inmueble (como Unidad).
        modelBuilder.Entity<Inmueble>()
            .HasMany(i => i.Medidores)
            .WithOne(m => m.Inmueble)
            .HasForeignKey(m => m.InmuebleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Inquilinos vinculados a un medidor — cascada al borrar el medidor.
        modelBuilder.Entity<Medidor>()
            .HasMany(m => m.Inquilinos)
            .WithOne(mi => mi.Medidor)
            .HasForeignKey(mi => mi.MedidorId)
            .OnDelete(DeleteBehavior.Cascade);

        // El vínculo referencia un Inquilino — RESTRICT (no borrar inquilino con medidores).
        modelBuilder.Entity<MedidorInquilino>()
            .HasOne(mi => mi.Inquilino)
            .WithMany()
            .HasForeignKey(mi => mi.InquilinoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Facturas/planillas de un medidor — cascada al borrar el medidor.
        modelBuilder.Entity<Medidor>()
            .HasMany(m => m.Facturas)
            .WithOne(f => f.Medidor)
            .HasForeignKey(f => f.MedidorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Desglose de servicios por pago — cascada al borrar el pago.
        modelBuilder.Entity<Pago>()
            .HasMany(p => p.DetalleServicios)
            .WithOne(d => d.Pago)
            .HasForeignKey(d => d.PagoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
