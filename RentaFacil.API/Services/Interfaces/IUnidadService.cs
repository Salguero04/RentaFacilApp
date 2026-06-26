using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IUnidadService
{
    Task<IEnumerable<UnidadDto>> GetAllAsync(int usuarioId);
    Task<UnidadDto?> CrearAsync(CrearUnidadDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearUnidadDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
