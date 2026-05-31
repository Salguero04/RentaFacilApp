using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetAllAsync();
    Task<Inquilino?> GetByIdAsync(int id);
    Task<Inquilino> AddAsync(Inquilino inquilino);
    Task UpdateAsync(Inquilino inquilino);
    Task DeleteAsync(int id);
}
