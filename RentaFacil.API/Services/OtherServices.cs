using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repository;
    private readonly IInquilinoRepository _inquilinoRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly INotificacionPendienteRepository _notificacionRepository;

    public ContratoService(IContratoRepository repository, IInquilinoRepository inquilinoRepository, IUnidadRepository unidadRepository, INotificacionPendienteRepository notificacionRepository)
    {
        _repository = repository;
        _inquilinoRepository = inquilinoRepository;
        _unidadRepository = unidadRepository;
        _notificacionRepository = notificacionRepository;
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
            Monto = dto.Monto, Garantia = dto.Garantia, Frecuencia = dto.Frecuencia,
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
        contrato.Monto = dto.Monto; contrato.Garantia = dto.Garantia; contrato.Frecuencia = dto.Frecuencia;
        contrato.DuracionMeses = dto.DuracionMeses; contrato.DiaPago = dto.DiaPago;
        contrato.FechaInicio = dto.FechaInicio; contrato.FechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses);
        contrato.Observaciones = dto.Observaciones;
        await _repository.UpdateAsync(contrato);

        // El inquilino tendrá la app a futuro: dejamos registro del cambio para notificarlo.
        await _notificacionRepository.AddAsync(new NotificacionPendiente
        {
            ContratoId = contrato.Id, InquilinoId = contrato.InquilinoId,
            Tipo = "ContratoEditado", Detalle = "El contrato fue modificado por el arrendador.",
            Fecha = DateTime.Now, Notificado = false, UsuarioId = usuarioId
        });
        return true;
    }
    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private async Task<bool> PertenecenAlUsuario(int inquilinoId, int unidadId, int usuarioId)
    {
        var inquilino = await _inquilinoRepository.GetByIdAsync(inquilinoId, usuarioId);
        var unidad = await _unidadRepository.GetByIdAsync(unidadId, usuarioId);
        return inquilino != null && unidad != null;
    }

    private static ContratoDto MapToDto(Contrato c) => new(
        c.Id, c.InquilinoId, c.UnidadId, c.Monto, c.Garantia, c.Frecuencia, c.DuracionMeses, c.DiaPago, c.FechaInicio, c.FechaFin, c.Observaciones, c.Activo);
}

public class PagoService : IPagoService
{
    private readonly IPagoRepository _repository;
    private readonly IContratoRepository _contratoRepository;

    public PagoService(IPagoRepository repository, IContratoRepository contratoRepository)
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
    }

    public async Task<IEnumerable<PagoDto>> GetAllAsync(int usuarioId)
    {
        var pagos = await _repository.GetAllAsync(usuarioId);
        return pagos.Select(MapToDto);
    }
    public async Task<PagoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var pago = await _repository.GetByIdAsync(id, usuarioId);
        return pago != null ? MapToDto(pago) : null;
    }
    public async Task<PagoDto?> CrearAsync(CrearPagoDto dto, int usuarioId)
    {
        var contrato = await _contratoRepository.GetByIdAsync(dto.ContratoId, usuarioId);
        if (contrato == null) return null;

        // El total de servicios sale del desglose si viene; si no, del campo plano (compat).
        var serviciosTotal = dto.Detalles != null && dto.Detalles.Count > 0
            ? dto.Detalles.Sum(d => d.Monto)
            : dto.Servicios;

        var pago = new Pago
        {
            ContratoId = dto.ContratoId, TotalMonto = dto.TotalMonto,
            ACuenta = dto.ACuenta, Servicios = serviciosTotal,
            FechaPago = dto.FechaPago, Periodo = dto.Periodo,
            // Completado considera renta + servicios (igual que el "restante" del recibo).
            Facturado = dto.Facturado, Completado = dto.ACuenta >= dto.TotalMonto + serviciosTotal, UsuarioId = usuarioId
        };
        if (dto.Detalles != null)
        {
            foreach (var d in dto.Detalles)
            {
                pago.DetalleServicios.Add(new DetalleServicioPago { Tipo = d.Tipo, Monto = d.Monto, UsuarioId = usuarioId });
            }
        }
        var created = await _repository.AddAsync(pago);
        return MapToDto(created);
    }
    public async Task<bool> UpdateAsync(int id, CrearPagoDto dto, int usuarioId)
    {
        var pago = await _repository.GetByIdAsync(id, usuarioId);
        if (pago == null) return false;

        var contrato = await _contratoRepository.GetByIdAsync(dto.ContratoId, usuarioId);
        if (contrato == null) return false;

        pago.ContratoId = dto.ContratoId; pago.TotalMonto = dto.TotalMonto;
        pago.ACuenta = dto.ACuenta; pago.Servicios = dto.Servicios;
        pago.FechaPago = dto.FechaPago; pago.Periodo = dto.Periodo;
        pago.Facturado = dto.Facturado;
        pago.Completado = dto.ACuenta >= dto.TotalMonto + dto.Servicios;
        await _repository.UpdateAsync(pago);
        return true;
    }
    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private static PagoDto MapToDto(Pago p) => new(
        p.Id, p.ContratoId, p.TotalMonto, p.ACuenta, p.Servicios, p.FechaPago, p.Periodo, p.Facturado, p.Completado,
        p.DetalleServicios?.Select(d => new DetalleServicioPagoDto(d.Tipo, d.Monto)).ToList());
}

public class RecordatorioService : IRecordatorioService
{
    private readonly IRecordatorioRepository _repository;
    private readonly IInquilinoRepository _inquilinoRepository;

    public RecordatorioService(IRecordatorioRepository repository, IInquilinoRepository inquilinoRepository)
    {
        _repository = repository;
        _inquilinoRepository = inquilinoRepository;
    }

    public async Task<IEnumerable<RecordatorioDto>> GetAllAsync(int usuarioId)
    {
        var recordatorios = await _repository.GetAllAsync(usuarioId);
        return recordatorios.Select(MapToDto);
    }

    public async Task<RecordatorioDto?> CrearAsync(CrearRecordatorioDto dto, int usuarioId)
    {
        var inquilino = await _inquilinoRepository.GetByIdAsync(dto.InquilinoId, usuarioId);
        if (inquilino == null) return null;

        var recordatorio = new Recordatorio
        {
            InquilinoId = dto.InquilinoId, ContratoId = dto.ContratoId,
            Detalle = dto.Detalle, FechaProgramada = dto.FechaProgramada, UsuarioId = usuarioId
        };
        var created = await _repository.AddAsync(recordatorio);
        return MapToDto(created);
    }

    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private static RecordatorioDto MapToDto(Recordatorio r) => new(r.Id, r.InquilinoId, r.ContratoId, r.Detalle, r.FechaProgramada);
}
