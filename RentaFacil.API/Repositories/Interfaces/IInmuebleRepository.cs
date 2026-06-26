using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInmuebleRepository
{
    Task<IEnumerable<Inmueble>> GetAllAsync(int usuarioId);
    Task<Inmueble?> GetByIdAsync(int id, int usuarioId);
    Task<Inmueble> AddAsync(Inmueble inmueble);
    Task UpdateAsync(Inmueble inmueble);
    Task DeleteAsync(int id, int usuarioId);
}
