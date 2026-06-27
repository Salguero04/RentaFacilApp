using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IContratoService
{
    Task<IEnumerable<ContratoDto>> GetAllAsync(int usuarioId);
    Task<ContratoDto?> GetByIdAsync(int id, int usuarioId);
    Task<ContratoDto?> CrearAsync(CrearContratoDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearContratoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IPagoService
{
    Task<IEnumerable<PagoDto>> GetAllAsync(int usuarioId);
    Task<PagoDto?> GetByIdAsync(int id, int usuarioId);
    Task<PagoDto?> CrearAsync(CrearPagoDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearPagoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IRecordatorioService
{
    Task<IEnumerable<RecordatorioDto>> GetAllAsync(int usuarioId);
    Task<RecordatorioDto?> CrearAsync(CrearRecordatorioDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
