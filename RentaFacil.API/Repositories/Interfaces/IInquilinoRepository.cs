using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetAllAsync(int usuarioId);
    Task<Inquilino?> GetByIdAsync(int id, int usuarioId);
    Task<Inquilino> AddAsync(Inquilino inquilino);
    Task UpdateAsync(Inquilino inquilino);
    Task DeleteAsync(int id, int usuarioId);
}
