using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByGoogleIdAsync(string googleId);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<Usuario> AddAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task<bool> ExisteAlgunoAsync();
}
