using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IContratoRepository
{
    Task<IEnumerable<Contrato>> GetAllAsync(int usuarioId);
    Task<Contrato?> GetByIdAsync(int id, int usuarioId);
    Task<Contrato> AddAsync(Contrato contrato);
    Task UpdateAsync(Contrato contrato);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetAllAsync(int usuarioId);
    Task<Pago?> GetByIdAsync(int id, int usuarioId);
    Task<Pago> AddAsync(Pago pago);
    Task UpdateAsync(Pago pago);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IRecordatorioRepository
{
    Task<IEnumerable<Recordatorio>> GetAllAsync(int usuarioId);
    Task<Recordatorio> AddAsync(Recordatorio recordatorio);
    Task DeleteAsync(int id, int usuarioId);
}
