using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

// Vista de solo lectura del inquilino sobre sus propios datos (api/mi/*). Toda la seguridad
// nace de `cuentaId` (el UsuarioId del token, rol Inquilino): el service resuelve primero qué
// Inquilinos están vinculados a esa cuenta y solo entonces consulta sus contratos/pagos/etc.
public interface IPortalInquilinoService
{
    Task<IEnumerable<MiContratoDto>> GetContratosAsync(int cuentaId);
    Task<IEnumerable<MiPagoDto>> GetPagosAsync(int cuentaId);

    // null = el pago no pertenece a ningún contrato de los inquilinos vinculados a esta cuenta (404).
    Task<byte[]?> GetReciboPagoAsync(int pagoId, int cuentaId, string formato);

    Task<IEnumerable<MiConsumoDto>> GetConsumosAsync(int cuentaId);
    Task<IEnumerable<MiNotificacionDto>> GetNotificacionesAsync(int cuentaId);

    // false = la notificación no existe o no pertenece a ningún inquilino vinculado a esta cuenta.
    Task<bool> MarcarNotificacionLeidaAsync(int id, int cuentaId);
}
