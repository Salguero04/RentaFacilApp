using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class InquilinoService : IInquilinoService
{
    private readonly IInquilinoRepository _repository;

    public InquilinoService(IInquilinoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<InquilinoDto>> GetAllAsync()
    {
        var inquilinos = await _repository.GetAllAsync();
        return inquilinos.Select(MapToDto);
    }

    public async Task<InquilinoDto?> GetByIdAsync(int id)
    {
        var inquilino = await _repository.GetByIdAsync(id);
        return inquilino != null ? MapToDto(inquilino) : null;
    }

    public async Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto)
    {
        var inquilino = new Inquilino
        {
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            FotoUrl = dto.FotoUrl,
            UsuarioId = dto.UsuarioId,
            FechaRegistro = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(inquilino);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, CrearInquilinoDto dto)
    {
        var inquilino = await _repository.GetByIdAsync(id);
        if (inquilino != null)
        {
            inquilino.NombreCompleto = dto.NombreCompleto;
            inquilino.Identificacion = dto.Identificacion;
            inquilino.Telefono = dto.Telefono;
            inquilino.FotoUrl = dto.FotoUrl;
            inquilino.UsuarioId = dto.UsuarioId;
            await _repository.UpdateAsync(inquilino);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static InquilinoDto MapToDto(Inquilino i)
    {
        return new InquilinoDto(i.Id, i.NombreCompleto, i.Identificacion, i.Telefono, i.FotoUrl, i.FechaRegistro, i.UsuarioId);
    }
}
