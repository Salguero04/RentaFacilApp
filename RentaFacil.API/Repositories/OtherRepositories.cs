using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class ContratoRepository : IContratoRepository
{
    private readonly AppDbContext _context;
    public ContratoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Contrato>> GetAllAsync(int usuarioId) => await _context.Contratos.Where(c => c.UsuarioId == usuarioId).ToListAsync();
    public async Task<Contrato?> GetByIdAsync(int id, int usuarioId) => await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
    public async Task<Contrato> AddAsync(Contrato contrato)
    {
        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();
        return contrato;
    }
    public async Task UpdateAsync(Contrato contrato)
    {
        _context.Contratos.Update(contrato);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id, int usuarioId)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (contrato != null)
        {
            _context.Contratos.Remove(contrato);
            await _context.SaveChangesAsync();
        }
    }
}

public class PagoRepository : IPagoRepository
{
    private readonly AppDbContext _context;
    public PagoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Pago>> GetAllAsync(int usuarioId) => await _context.Pagos.Include(p => p.DetalleServicios).Where(p => p.UsuarioId == usuarioId).ToListAsync();
    public async Task<Pago?> GetByIdAsync(int id, int usuarioId) => await _context.Pagos.Include(p => p.DetalleServicios).FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
    public async Task<Pago> AddAsync(Pago pago)
    {
        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();
        return pago;
    }
    public async Task UpdateAsync(Pago pago)
    {
        _context.Pagos.Update(pago);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id, int usuarioId)
    {
        var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
        if (pago != null)
        {
            _context.Pagos.Remove(pago);
            await _context.SaveChangesAsync();
        }
    }
}

public class RecordatorioRepository : IRecordatorioRepository
{
    private readonly AppDbContext _context;
    public RecordatorioRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Recordatorio>> GetAllAsync(int usuarioId) => await _context.Recordatorios.Where(r => r.UsuarioId == usuarioId).ToListAsync();
    public async Task<Recordatorio> AddAsync(Recordatorio recordatorio)
    {
        _context.Recordatorios.Add(recordatorio);
        await _context.SaveChangesAsync();
        return recordatorio;
    }
    public async Task DeleteAsync(int id, int usuarioId)
    {
        var recordatorio = await _context.Recordatorios.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);
        if (recordatorio != null)
        {
            _context.Recordatorios.Remove(recordatorio);
            await _context.SaveChangesAsync();
        }
    }
}
