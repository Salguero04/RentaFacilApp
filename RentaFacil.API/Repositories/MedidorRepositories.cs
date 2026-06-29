using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class MedidorRepository : IMedidorRepository
{
    private readonly AppDbContext _context;
    public MedidorRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Medidor>> GetAllAsync(int usuarioId) =>
        await _context.Medidores
            .Include(m => m.Inmueble)
            .Include(m => m.Inquilinos)
            .Where(m => m.UsuarioId == usuarioId)
            .ToListAsync();

    public async Task<Medidor?> GetByIdAsync(int id, int usuarioId) =>
        await _context.Medidores
            .Include(m => m.Inmueble)
            .Include(m => m.Inquilinos)
            .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

    public async Task<Medidor> AddAsync(Medidor medidor)
    {
        _context.Medidores.Add(medidor);
        await _context.SaveChangesAsync();
        return medidor;
    }

    public async Task UpdateAsync(Medidor medidor)
    {
        _context.Medidores.Update(medidor);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var medidor = await _context.Medidores.FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);
        if (medidor != null)
        {
            _context.Medidores.Remove(medidor);
            await _context.SaveChangesAsync();
        }
    }
}

public class MedidorInquilinoRepository : IMedidorInquilinoRepository
{
    private readonly AppDbContext _context;
    public MedidorInquilinoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<MedidorInquilino>> GetByMedidorAsync(int medidorId, int usuarioId) =>
        await _context.MedidoresInquilino
            .Include(mi => mi.Inquilino)
            .Where(mi => mi.MedidorId == medidorId && mi.UsuarioId == usuarioId)
            .ToListAsync();

    public async Task<IEnumerable<MedidorInquilino>> GetByContratoAsync(int contratoId, int usuarioId) =>
        await _context.MedidoresInquilino
            .Include(mi => mi.Medidor)
            .Where(mi => mi.ContratoId == contratoId && mi.UsuarioId == usuarioId)
            .ToListAsync();

    public async Task<MedidorInquilino?> GetByIdAsync(int id, int usuarioId) =>
        await _context.MedidoresInquilino.FirstOrDefaultAsync(mi => mi.Id == id && mi.UsuarioId == usuarioId);

    public async Task<MedidorInquilino> AddAsync(MedidorInquilino vinculo)
    {
        _context.MedidoresInquilino.Add(vinculo);
        await _context.SaveChangesAsync();
        return vinculo;
    }

    public async Task UpdateAsync(MedidorInquilino vinculo)
    {
        _context.MedidoresInquilino.Update(vinculo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var vinculo = await _context.MedidoresInquilino.FirstOrDefaultAsync(mi => mi.Id == id && mi.UsuarioId == usuarioId);
        if (vinculo != null)
        {
            _context.MedidoresInquilino.Remove(vinculo);
            await _context.SaveChangesAsync();
        }
    }
}

public class FacturaMedidorRepository : IFacturaMedidorRepository
{
    private readonly AppDbContext _context;
    public FacturaMedidorRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<FacturaMedidor>> GetAllAsync(int usuarioId) =>
        await _context.FacturasMedidor.Where(f => f.UsuarioId == usuarioId).ToListAsync();

    public async Task<IEnumerable<FacturaMedidor>> GetByMedidorAsync(int medidorId, int usuarioId) =>
        await _context.FacturasMedidor.Where(f => f.MedidorId == medidorId && f.UsuarioId == usuarioId).ToListAsync();

    public async Task<FacturaMedidor?> GetByClaveAsync(int medidorId, int mes, int anio, int usuarioId) =>
        await _context.FacturasMedidor.FirstOrDefaultAsync(f =>
            f.MedidorId == medidorId && f.Mes == mes && f.Anio == anio && f.UsuarioId == usuarioId);

    public async Task<FacturaMedidor> AddAsync(FacturaMedidor factura)
    {
        _context.FacturasMedidor.Add(factura);
        await _context.SaveChangesAsync();
        return factura;
    }

    public async Task UpdateAsync(FacturaMedidor factura)
    {
        _context.FacturasMedidor.Update(factura);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var factura = await _context.FacturasMedidor.FirstOrDefaultAsync(f => f.Id == id && f.UsuarioId == usuarioId);
        if (factura != null)
        {
            _context.FacturasMedidor.Remove(factura);
            await _context.SaveChangesAsync();
        }
    }
}

public class NotificacionPendienteRepository : INotificacionPendienteRepository
{
    private readonly AppDbContext _context;
    public NotificacionPendienteRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<NotificacionPendiente>> GetAllAsync(int usuarioId) =>
        await _context.NotificacionesPendientes.Where(n => n.UsuarioId == usuarioId).ToListAsync();

    public async Task<NotificacionPendiente> AddAsync(NotificacionPendiente notificacion)
    {
        _context.NotificacionesPendientes.Add(notificacion);
        await _context.SaveChangesAsync();
        return notificacion;
    }
}
