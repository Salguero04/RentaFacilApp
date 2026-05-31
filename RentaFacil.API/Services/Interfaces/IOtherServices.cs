using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IContratoService
{
    Task<IEnumerable<ContratoDto>> GetAllAsync();
    Task<ContratoDto?> GetByIdAsync(int id);
    Task<ContratoDto> CrearAsync(CrearContratoDto dto);
    Task UpdateAsync(int id, CrearContratoDto dto);
    Task DeleteAsync(int id);
}

public interface IPagoService
{
    Task<IEnumerable<PagoDto>> GetAllAsync();
    Task<PagoDto?> GetByIdAsync(int id);
    Task<PagoDto> CrearAsync(CrearPagoDto dto);
    Task UpdateAsync(int id, CrearPagoDto dto);
    Task DeleteAsync(int id);
}
