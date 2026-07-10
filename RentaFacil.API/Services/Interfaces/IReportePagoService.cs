using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

// "Ya pagué": lado inquilino (crear/listar los suyos, validando que el ContratoId esté entre
// SUS contratos vinculados) y lado arrendador (bandeja/confirmar/rechazar con ownership por
// IReportePagoRepository.GetByIdAsync(id, usuarioId)).
public interface IReportePagoService
{
    // null = el contrato no pertenece a ningún inquilino vinculado a esta cuenta, monto <= 0,
    // o la foto supera 1 MB.
    Task<ReportePagoDto?> CrearAsync(CrearReportePagoDto dto, int cuentaId);
    Task<IEnumerable<ReportePagoDto>> GetMisReportesAsync(int cuentaId);

    Task<IEnumerable<ReportePagoDto>> GetBandejaAsync(int usuarioId, EstadoReportePago? estado = null);

    // null = el reporte no existe, no pertenece a este arrendador, o no tiene foto adjunta.
    Task<byte[]?> GetComprobanteAsync(int id, int usuarioId);

    // false = no existe, no pertenece a este arrendador, o ya no está Pendiente (no se re-transiciona).
    Task<bool> ConfirmarAsync(int id, int usuarioId);
    Task<bool> RechazarAsync(int id, int usuarioId);
}
