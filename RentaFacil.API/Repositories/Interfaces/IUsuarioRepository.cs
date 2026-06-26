using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario> AddAsync(Usuario usuario);
    Task<bool> ExisteAlgunoAsync();
}
