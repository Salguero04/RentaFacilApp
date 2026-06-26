using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInmuebleService
{
    Task<IEnumerable<InmuebleDto>> GetAllAsync(int usuarioId);
    Task<InmuebleDto?> GetByIdAsync(int id, int usuarioId);
    Task<InmuebleDto> CrearAsync(CrearInmuebleDto dto, int usuarioId);
    Task UpdateAsync(int id, CrearInmuebleDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
