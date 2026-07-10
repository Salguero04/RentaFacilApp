using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

// "Ya pagué": lado inquilino (CrearAsync/GetMisReportesAsync, cuentaId = UsuarioId del token con
// rol Inquilino) y lado arrendador (GetBandejaAsync/GetComprobanteAsync/ConfirmarAsync/RechazarAsync,
// usuarioId = arrendador). El ContratoId de un reporte nuevo se valida contra la cadena
// cuenta→inquilinos→contratos de IPortalInquilinoRepository (igual que PortalInquilinoService) —
// nunca se confía en un ContratoId que venga del cliente sin validarlo contra esa cadena.
public class ReportePagoService : IReportePagoService
{
    private const long TamanioMaximoFotoBytes = 1_048_576; // 1 MB

    private readonly IReportePagoRepository _repository;
    private readonly IPortalInquilinoRepository _portalRepository;
    private readonly IInquilinoRepository _inquilinoRepository;
    private readonly IDataChangeNotifier _notifier;

    public ReportePagoService(
        IReportePagoRepository repository,
        IPortalInquilinoRepository portalRepository,
        IInquilinoRepository inquilinoRepository,
        IDataChangeNotifier notifier)
    {
        _repository = repository;
        _portalRepository = portalRepository;
        _inquilinoRepository = inquilinoRepository;
        _notifier = notifier;
    }

    public async Task<ReportePagoDto?> CrearAsync(CrearReportePagoDto dto, int cuentaId)
    {
        if (dto.Monto <= 0) return null;
        if (dto.FotoComprobante != null && dto.FotoComprobante.LongLength > TamanioMaximoFotoBytes) return null;

        var inquilinos = await _portalRepository.GetInquilinosPorCuentaAsync(cuentaId);
        var inquilinoIds = inquilinos.Select(i => i.Id).ToList();
        if (inquilinoIds.Count == 0) return null;

        var contratos = await _portalRepository.GetContratosPorInquilinosAsync(inquilinoIds);
        var contrato = contratos.FirstOrDefault(c => c.Id == dto.ContratoId);
        if (contrato == null) return null;

        var reporte = new ReportePago
        {
            ContratoId = contrato.Id,
            InquilinoId = contrato.InquilinoId,
            Monto = dto.Monto,
            Comentario = dto.Comentario,
            FotoComprobante = dto.FotoComprobante,
            FechaReporte = DateTime.UtcNow,
            Estado = EstadoReportePago.Pendiente,
            UsuarioId = contrato.UsuarioId,       // arrendador dueño del contrato
            CuentaInquilinoId = cuentaId
        };
        var creado = await _repository.AddAsync(reporte);

        // El arrendador lo ve llegar en tiempo real en su bandeja (SignalR, best-effort).
        await _notifier.NotificarCambioAsync("ReportePago", contrato.UsuarioId, "crear");

        var nombreInquilino = inquilinos.FirstOrDefault(i => i.Id == contrato.InquilinoId)?.NombreCompleto ?? string.Empty;
        return MapToDto(creado, nombreInquilino);
    }

    public async Task<IEnumerable<ReportePagoDto>> GetMisReportesAsync(int cuentaId)
    {
        var inquilinos = await _portalRepository.GetInquilinosPorCuentaAsync(cuentaId);
        var nombrePorInquilino = inquilinos.ToDictionary(i => i.Id, i => i.NombreCompleto);

        var reportes = await _repository.GetByCuentaInquilinoAsync(cuentaId);
        return reportes.Select(r => MapToDto(r, nombrePorInquilino.GetValueOrDefault(r.InquilinoId, string.Empty)));
    }

    public async Task<IEnumerable<ReportePagoDto>> GetBandejaAsync(int usuarioId, EstadoReportePago? estado = null)
    {
        var reportes = await _repository.GetByArrendadorAsync(usuarioId);
        if (estado.HasValue)
        {
            reportes = reportes.Where(r => r.Estado == estado.Value);
        }

        var dtos = new List<ReportePagoDto>();
        foreach (var reporte in reportes)
        {
            var inquilino = await _inquilinoRepository.GetByIdAsync(reporte.InquilinoId, usuarioId);
            dtos.Add(MapToDto(reporte, inquilino?.NombreCompleto ?? string.Empty));
        }
        return dtos;
    }

    public async Task<byte[]?> GetComprobanteAsync(int id, int usuarioId)
    {
        var reporte = await _repository.GetByIdAsync(id, usuarioId);
        return reporte?.FotoComprobante;
    }

    public Task<bool> ConfirmarAsync(int id, int usuarioId) => CambiarEstadoAsync(id, usuarioId, EstadoReportePago.Confirmado);

    public Task<bool> RechazarAsync(int id, int usuarioId) => CambiarEstadoAsync(id, usuarioId, EstadoReportePago.Rechazado);

    private async Task<bool> CambiarEstadoAsync(int id, int usuarioId, EstadoReportePago nuevoEstado)
    {
        var reporte = await _repository.GetByIdAsync(id, usuarioId);
        if (reporte == null || reporte.Estado != EstadoReportePago.Pendiente) return false;

        reporte.Estado = nuevoEstado;
        await _repository.UpdateAsync(reporte);
        return true;
    }

    private static ReportePagoDto MapToDto(ReportePago r, string nombreInquilino) =>
        new(r.Id, r.ContratoId, r.InquilinoId, nombreInquilino, r.Monto, r.Comentario, r.FotoComprobante != null, r.FechaReporte, r.Estado);
}
