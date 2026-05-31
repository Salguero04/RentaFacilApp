using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IContratoRepository
{
    Task<IEnumerable<Contrato>> GetAllAsync();
    Task<Contrato?> GetByIdAsync(int id);
    Task<Contrato> AddAsync(Contrato contrato);
    Task UpdateAsync(Contrato contrato);
    Task DeleteAsync(int id);
}

public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetAllAsync();
    Task<Pago?> GetByIdAsync(int id);
    Task<Pago> AddAsync(Pago pago);
    Task UpdateAsync(Pago pago);
    Task DeleteAsync(int id);
}
