using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class InquilinoRepository : IInquilinoRepository
{
    private readonly AppDbContext _context;

    public InquilinoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Inquilino>> GetAllAsync(int usuarioId)
    {
        return await _context.Inquilinos.Where(i => i.UsuarioId == usuarioId).ToListAsync();
    }

    public async Task<Inquilino?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Inquilinos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    }

    public async Task<Inquilino> AddAsync(Inquilino inquilino)
    {
        _context.Inquilinos.Add(inquilino);
        await _context.SaveChangesAsync();
        return inquilino;
    }

    public async Task UpdateAsync(Inquilino inquilino)
    {
        _context.Inquilinos.Update(inquilino);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var inquilino = await _context.Inquilinos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
        if (inquilino != null)
        {
            _context.Inquilinos.Remove(inquilino);
            await _context.SaveChangesAsync();
        }
    }
}
