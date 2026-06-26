using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repository;
    private readonly IInquilinoRepository _inquilinoRepository;
    private readonly IUnidadRepository _unidadRepository;

    public ContratoService(IContratoRepository repository, IInquilinoRepository inquilinoRepository, IUnidadRepository unidadRepository)
    {
        _repository = repository;
        _inquilinoRepository = inquilinoRepository;
        _unidadRepository = unidadRepository;
    }

    public async Task<IEnumerable<ContratoDto>> GetAllAsync(int usuarioId)
    {
        var contratos = await _repository.GetAllAsync(usuarioId);
        return contratos.Select(MapToDto);
    }
    public async Task<ContratoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var contrato = await _repository.GetByIdAsync(id, usuarioId);
        return contrato != null ? MapToDto(contrato) : null;
    }
    public async Task<ContratoDto?> CrearAsync(CrearContratoDto dto, int usuarioId)
    {
        if (!await PertenecenAlUsuario(dto.InquilinoId, dto.UnidadId, usuarioId)) return null;

        var contrato = new Contrato
        {
            InquilinoId = dto.InquilinoId, UnidadId = dto.UnidadId,
            Monto = dto.Monto, Garantia = dto.Garantia,
            DuracionMeses = dto.DuracionMeses, DiaPago = dto.DiaPago,
            FechaInicio = dto.FechaInicio, FechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses),
            Observaciones = dto.Observaciones, Activo = true, UsuarioId = usuarioId
        };
        var created = await _repository.AddAsync(contrato);
        return MapToDto(created);
    }
    public async Task<bool> UpdateAsync(int id, CrearContratoDto dto, int usuarioId)
    {
        var contrato = await _repository.GetByIdAsync(id, usuarioId);
        if (contrato == null) return false;
        if (!await PertenecenAlUsuario(dto.InquilinoId, dto.UnidadId, usuarioId)) return false;

        contrato.InquilinoId = dto.InquilinoId; contrato.UnidadId = dto.UnidadId;
        contrato.Monto = dto.Monto; contrato.Garantia = dto.Garantia;
        contrato.DuracionMeses = dto.DuracionMeses; contrato.DiaPago = dto.DiaPago;
        contrato.FechaInicio = dto.FechaInicio; contrato.FechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses);
        contrato.Observaciones = dto.Observaciones;
        await _repository.UpdateAsync(contrato);
        return true;
    }
    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private async Task<bool> PertenecenAlUsuario(int inquilinoId, int unidadId, int usuarioId)
    {
        var inquilino = await _inquilinoRepository.GetByIdAsync(inquilinoId, usuarioId);
        var unidad = await _unidadRepository.GetByIdAsync(unidadId, usuarioId);
        return inquilino != null && unidad != null;
    }

    private static ContratoDto MapToDto(Contrato c) => new(c.Id, c.InquilinoId, c.UnidadId, c.Monto, c.Garantia, c.DuracionMeses, c.DiaPago, c.FechaInicio, c.FechaFin, c.Observaciones, c.Activo);
}

public class PagoService : IPagoService
{
    private readonly IPagoRepository _repository;
    public PagoService(IPagoRepository repository) => _repository = repository;

    public async Task<IEnumerable<PagoDto>> GetAllAsync()
    {
        var pagos = await _repository.GetAllAsync();
        return pagos.Select(MapToDto);
    }
    public async Task<PagoDto?> GetByIdAsync(int id)
    {
        var pago = await _repository.GetByIdAsync(id);
        return pago != null ? MapToDto(pago) : null;
    }
    public async Task<PagoDto> CrearAsync(CrearPagoDto dto)
    {
        var pago = new Pago
        {
            ContratoId = dto.ContratoId, TotalMonto = dto.TotalMonto,
            ACuenta = dto.ACuenta, Servicios = dto.Servicios,
            FechaPago = dto.FechaPago, Periodo = dto.Periodo,
            Facturado = false, Completado = dto.ACuenta >= dto.TotalMonto
        };
        var created = await _repository.AddAsync(pago);
        return MapToDto(created);
    }
    public async Task UpdateAsync(int id, CrearPagoDto dto)
    {
        var pago = await _repository.GetByIdAsync(id);
        if (pago != null)
        {
            pago.ContratoId = dto.ContratoId; pago.TotalMonto = dto.TotalMonto;
            pago.ACuenta = dto.ACuenta; pago.Servicios = dto.Servicios;
            pago.FechaPago = dto.FechaPago; pago.Periodo = dto.Periodo;
            pago.Completado = dto.ACuenta >= dto.TotalMonto;
            await _repository.UpdateAsync(pago);
        }
    }
    public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);

    private static PagoDto MapToDto(Pago p) => new(p.Id, p.ContratoId, p.TotalMonto, p.ACuenta, p.Servicios, p.FechaPago, p.Periodo, p.Facturado, p.Completado);
}
