using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class InmuebleRepository : IInmuebleRepository
{
    private readonly AppDbContext _context;

    public InmuebleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Inmueble>> GetAllAsync(int usuarioId)
    {
        return await _context.Inmuebles.Where(i => i.UsuarioId == usuarioId).ToListAsync();
    }

    public async Task<Inmueble?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Inmuebles.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    }

    public async Task<Inmueble> AddAsync(Inmueble inmueble)
    {
        _context.Inmuebles.Add(inmueble);
        await _context.SaveChangesAsync();
        return inmueble;
    }

    public async Task UpdateAsync(Inmueble inmueble)
    {
        _context.Inmuebles.Update(inmueble);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var inmueble = await _context.Inmuebles.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
        if (inmueble != null)
        {
            _context.Inmuebles.Remove(inmueble);
            await _context.SaveChangesAsync();
        }
    }
}
