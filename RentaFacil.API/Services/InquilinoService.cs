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

    public async Task<IEnumerable<InquilinoDto>> GetAllAsync(int usuarioId)
    {
        var inquilinos = await _repository.GetAllAsync(usuarioId);
        return inquilinos.Select(MapToDto);
    }

    public async Task<InquilinoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var inquilino = await _repository.GetByIdAsync(id, usuarioId);
        return inquilino != null ? MapToDto(inquilino) : null;
    }

    public async Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto, int usuarioId)
    {
        var inquilino = new Inquilino
        {
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            FotoUrl = dto.FotoUrl,
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(inquilino);
        return MapToDto(created);
    }

    public async Task<bool> UpdateAsync(int id, CrearInquilinoDto dto, int usuarioId)
    {
        var inquilino = await _repository.GetByIdAsync(id, usuarioId);
        if (inquilino == null) return false;

        inquilino.NombreCompleto = dto.NombreCompleto;
        inquilino.Identificacion = dto.Identificacion;
        inquilino.Telefono = dto.Telefono;
        inquilino.FotoUrl = dto.FotoUrl;
        await _repository.UpdateAsync(inquilino);
        return true;
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static InquilinoDto MapToDto(Inquilino i)
    {
        return new InquilinoDto(i.Id, i.NombreCompleto, i.Identificacion, i.Telefono, i.FotoUrl, i.FechaRegistro, i.UsuarioId);
    }
}
