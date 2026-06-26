using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class UnidadRepository : IUnidadRepository
{
    private readonly AppDbContext _context;

    public UnidadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Unidad>> GetAllAsync(int usuarioId)
    {
        return await _context.Unidades.Where(u => u.UsuarioId == usuarioId).ToListAsync();
    }

    public async Task<Unidad?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.UsuarioId == usuarioId);
    }

    public async Task<Unidad> AddAsync(Unidad unidad)
    {
        _context.Unidades.Add(unidad);
        await _context.SaveChangesAsync();
        return unidad;
    }

    public async Task UpdateAsync(Unidad unidad)
    {
        _context.Unidades.Update(unidad);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var unidad = await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.UsuarioId == usuarioId);
        if (unidad != null)
        {
            _context.Unidades.Remove(unidad);
            await _context.SaveChangesAsync();
        }
    }
}
