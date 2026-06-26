using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;
    public UsuarioRepository(AppDbContext context) => _context = context;

    public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

    public async Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<bool> ExisteAlgunoAsync() => await _context.Usuarios.AnyAsync();
}
