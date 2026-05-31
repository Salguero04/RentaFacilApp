using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class ContratoRepository : IContratoRepository
{
    private readonly AppDbContext _context;
    public ContratoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Contrato>> GetAllAsync() => await _context.Contratos.ToListAsync();
    public async Task<Contrato?> GetByIdAsync(int id) => await _context.Contratos.FirstOrDefaultAsync(i => i.Id == id);
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
    public async Task DeleteAsync(int id)
    {
        var contrato = await _context.Contratos.FindAsync(id);
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

    public async Task<IEnumerable<Pago>> GetAllAsync() => await _context.Pagos.ToListAsync();
    public async Task<Pago?> GetByIdAsync(int id) => await _context.Pagos.FirstOrDefaultAsync(i => i.Id == id);
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
    public async Task DeleteAsync(int id)
    {
        var pago = await _context.Pagos.FindAsync(id);
        if (pago != null)
        {
            _context.Pagos.Remove(pago);
            await _context.SaveChangesAsync();
        }
    }
}
