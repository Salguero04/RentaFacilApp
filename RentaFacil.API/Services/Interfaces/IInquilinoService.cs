using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInquilinoService
{
    Task<IEnumerable<InquilinoDto>> GetAllAsync(int usuarioId);
    Task<InquilinoDto?> GetByIdAsync(int id, int usuarioId);
    Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearInquilinoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
