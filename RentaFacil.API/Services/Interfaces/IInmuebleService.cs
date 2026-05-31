using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInmuebleService
{
    Task<IEnumerable<InmuebleDto>> GetAllAsync();
    Task<InmuebleDto?> GetByIdAsync(int id);
    Task<InmuebleDto> CrearAsync(CrearInmuebleDto dto);
    Task UpdateAsync(int id, CrearInmuebleDto dto);
    Task DeleteAsync(int id);
}
