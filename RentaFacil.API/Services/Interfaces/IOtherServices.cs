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
    Task<IEnumerable<PagoDto>> GetAllAsync();
    Task<PagoDto?> GetByIdAsync(int id);
    Task<PagoDto> CrearAsync(CrearPagoDto dto);
    Task UpdateAsync(int id, CrearPagoDto dto);
    Task DeleteAsync(int id);
}
