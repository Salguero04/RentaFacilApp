using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IUnidadRepository
{
    Task<IEnumerable<Unidad>> GetAllAsync(int usuarioId);
    Task<Unidad?> GetByIdAsync(int id, int usuarioId);
    Task<Unidad> AddAsync(Unidad unidad);
    Task UpdateAsync(Unidad unidad);
    Task DeleteAsync(int id, int usuarioId);
}
