using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

// Ver la nota de seguridad en IPortalInquilinoRepository: este Service es el único punto
// autorizado a llamarlo. Cada método parte SIEMPRE de `cuentaId` (el UsuarioId del token de
// una cuenta con rol Inquilino) para derivar los InquilinoIds/ContratoIds permitidos antes de
// leer nada — nunca se confía en un Id que venga del cliente sin validarlo contra esa cadena.
public class PortalInquilinoService : IPortalInquilinoService
{
    private readonly IPortalInquilinoRepository _portalRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IReciboService _reciboService;

    public PortalInquilinoService(
        IPortalInquilinoRepository portalRepository,
        IUsuarioRepository usuarioRepository,
        IReciboService reciboService)
    {
        _portalRepository = portalRepository;
        _usuarioRepository = usuarioRepository;
        _reciboService = reciboService;
    }

    public async Task<IEnumerable<MiContratoDto>> GetContratosAsync(int cuentaId)
    {
        var inquilinoIds = await ObtenerInquilinoIdsAsync(cuentaId);
        if (inquilinoIds.Count == 0)
        {
            return Enumerable.Empty<MiContratoDto>();
        }

        var contratos = await _portalRepository.GetContratosPorInquilinosAsync(inquilinoIds);

        var dtos = new List<MiContratoDto>();
        foreach (var contrato in contratos)
        {
            var arrendador = await _usuarioRepository.GetByIdAsync(contrato.UsuarioId);
            dtos.Add(new MiContratoDto(
                contrato.Id,
                arrendador?.NombreUsuario ?? string.Empty,
                contrato.Unidad?.Nombre ?? string.Empty,
                contrato.Unidad?.Inmueble?.Nombre ?? string.Empty,
                contrato.Monto,
                contrato.Frecuencia,
                contrato.DiaPago,
                contrato.FechaInicio,
                contrato.FechaFin,
                contrato.Activo));
        }

        return dtos;
    }

    public async Task<IEnumerable<MiPagoDto>> GetPagosAsync(int cuentaId)
    {
        var pagos = await ObtenerPagosDeCuentaAsync(cuentaId);
        return pagos.Select(p => new MiPagoDto(p.Id, p.ContratoId, p.Periodo, p.TotalMonto, p.ACuenta, p.Servicios, p.FechaPago, p.Completado));
    }

    public async Task<byte[]?> GetReciboPagoAsync(int pagoId, int cuentaId, string formato)
    {
        var pagos = await ObtenerPagosDeCuentaAsync(cuentaId);
        var pago = pagos.FirstOrDefault(p => p.Id == pagoId);
        if (pago == null)
        {
            return null;
        }

        // pago.UsuarioId es el arrendador dueño del pago (todas las filas de renta.* lo tienen) —
        // ya validamos arriba que el pago pertenece a un contrato de un inquilino vinculado a esta
        // cuenta, así que reusar IReciboService con ese UsuarioId es seguro.
        return await _reciboService.GenerarReciboPdfAsync(pago.Id, pago.UsuarioId, formato);
    }

    public async Task<IEnumerable<MiConsumoDto>> GetConsumosAsync(int cuentaId)
    {
        var inquilinoIds = await ObtenerInquilinoIdsAsync(cuentaId);
        if (inquilinoIds.Count == 0)
        {
            return Enumerable.Empty<MiConsumoDto>();
        }

        var vinculos = await _portalRepository.GetVinculosMedidorPorInquilinosAsync(inquilinoIds);
        return vinculos.Select(v => new MiConsumoDto(v.Medidor.Nombre, v.Medidor.Tipo, v.LecturaAnterior, v.LecturaActual, v.MetodoCobro));
    }

    public async Task<IEnumerable<MiNotificacionDto>> GetNotificacionesAsync(int cuentaId)
    {
        var inquilinoIds = await ObtenerInquilinoIdsAsync(cuentaId);
        if (inquilinoIds.Count == 0)
        {
            return Enumerable.Empty<MiNotificacionDto>();
        }

        var notificaciones = await _portalRepository.GetNotificacionesPorInquilinosAsync(inquilinoIds);
        return notificaciones.Select(n => new MiNotificacionDto(n.Id, n.Tipo, n.Detalle, n.Fecha, n.Notificado));
    }

    public async Task<bool> MarcarNotificacionLeidaAsync(int id, int cuentaId)
    {
        var notificacion = await _portalRepository.GetNotificacionAsync(id);
        if (notificacion == null)
        {
            return false;
        }

        var inquilinoIds = await ObtenerInquilinoIdsAsync(cuentaId);
        if (!inquilinoIds.Contains(notificacion.InquilinoId))
        {
            return false;
        }

        notificacion.Notificado = true;
        await _portalRepository.MarcarNotificadaAsync(notificacion);
        return true;
    }

    private async Task<List<int>> ObtenerInquilinoIdsAsync(int cuentaId)
    {
        var inquilinos = await _portalRepository.GetInquilinosPorCuentaAsync(cuentaId);
        return inquilinos.Select(i => i.Id).ToList();
    }

    private async Task<List<Pago>> ObtenerPagosDeCuentaAsync(int cuentaId)
    {
        var inquilinoIds = await ObtenerInquilinoIdsAsync(cuentaId);
        if (inquilinoIds.Count == 0)
        {
            return new List<Pago>();
        }

        var contratos = await _portalRepository.GetContratosPorInquilinosAsync(inquilinoIds);
        var contratoIds = contratos.Select(c => c.Id).ToList();
        if (contratoIds.Count == 0)
        {
            return new List<Pago>();
        }

        var pagos = await _portalRepository.GetPagosPorContratosAsync(contratoIds);
        return pagos.ToList();
    }
}
