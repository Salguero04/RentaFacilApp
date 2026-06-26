using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class UnidadService : IUnidadService
{
    private readonly IUnidadRepository _repository;
    private readonly IInmuebleRepository _inmuebleRepository;

    public UnidadService(IUnidadRepository repository, IInmuebleRepository inmuebleRepository)
    {
        _repository = repository;
        _inmuebleRepository = inmuebleRepository;
    }

    public async Task<IEnumerable<UnidadDto>> GetAllAsync(int usuarioId)
    {
        var unidades = await _repository.GetAllAsync(usuarioId);
        return unidades.Select(MapToDto);
    }

    public async Task<UnidadDto?> CrearAsync(CrearUnidadDto dto, int usuarioId)
    {
        var inmueble = await _inmuebleRepository.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return null;

        var unidad = new Unidad
        {
            Nombre = dto.Nombre,
            MontoRenta = dto.MontoRenta,
            InmuebleId = dto.InmuebleId,
            Ocupada = false,
            UsuarioId = usuarioId
        };

        var created = await _repository.AddAsync(unidad);
        return MapToDto(created);
    }

    public async Task<bool> UpdateAsync(int id, CrearUnidadDto dto, int usuarioId)
    {
        var unidad = await _repository.GetByIdAsync(id, usuarioId);
        if (unidad == null) return false;

        var inmueble = await _inmuebleRepository.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return false;

        unidad.Nombre = dto.Nombre;
        unidad.MontoRenta = dto.MontoRenta;
        unidad.InmuebleId = dto.InmuebleId;
        await _repository.UpdateAsync(unidad);
        return true;
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static UnidadDto MapToDto(Unidad u)
    {
        return new UnidadDto(u.Id, u.Nombre, u.MontoRenta, u.Ocupada, u.InmuebleId);
    }
}
