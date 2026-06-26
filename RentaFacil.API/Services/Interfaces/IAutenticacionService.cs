using RentaFacil.API.Models;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IAutenticacionService
{
    Task<LoginResultDto?> LoginAsync(LoginDto dto);
    Task<Usuario> RegistrarAsync(RegistrarUsuarioDto dto);
}
