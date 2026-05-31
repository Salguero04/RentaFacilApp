using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInmuebleRepository
{
    Task<IEnumerable<Inmueble>> GetAllAsync();
    Task<Inmueble?> GetByIdAsync(int id);
    Task<Inmueble> AddAsync(Inmueble inmueble);
    Task UpdateAsync(Inmueble inmueble);
    Task DeleteAsync(int id);
}
