using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInquilinoService
{
    Task<IEnumerable<InquilinoDto>> GetAllAsync();
    Task<InquilinoDto?> GetByIdAsync(int id);
    Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto);
    Task UpdateAsync(int id, CrearInquilinoDto dto);
    Task DeleteAsync(int id);
}
