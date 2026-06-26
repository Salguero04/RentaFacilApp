using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class InmuebleService : IInmuebleService
{
    private readonly IInmuebleRepository _repository;

    public InmuebleService(IInmuebleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<InmuebleDto>> GetAllAsync(int usuarioId)
    {
        var inmuebles = await _repository.GetAllAsync(usuarioId);
        return inmuebles.Select(MapToDto);
    }

    public async Task<InmuebleDto?> GetByIdAsync(int id, int usuarioId)
    {
        var inmueble = await _repository.GetByIdAsync(id, usuarioId);
        return inmueble != null ? MapToDto(inmueble) : null;
    }

    public async Task<InmuebleDto> CrearAsync(CrearInmuebleDto dto, int usuarioId)
    {
        var inmueble = new Inmueble
        {
            Nombre = dto.Nombre,
            Direccion = dto.Direccion,
            Tipo = dto.Tipo,
            MontoRenta = dto.MontoRenta,
            UsuarioId = usuarioId
        };

        var created = await _repository.AddAsync(inmueble);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, CrearInmuebleDto dto, int usuarioId)
    {
        var inmueble = await _repository.GetByIdAsync(id, usuarioId);
        if (inmueble != null)
        {
            inmueble.Nombre = dto.Nombre;
            inmueble.Direccion = dto.Direccion;
            inmueble.Tipo = dto.Tipo;
            inmueble.MontoRenta = dto.MontoRenta;
            await _repository.UpdateAsync(inmueble);
        }
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static InmuebleDto MapToDto(Inmueble i)
    {
        return new InmuebleDto(i.Id, i.Nombre, i.Direccion, i.Tipo, i.MontoRenta, i.UsuarioId);
    }
}
