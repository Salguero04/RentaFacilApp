using Microsoft.AspNetCore.SignalR;
using RentaFacil.API.Hubs;
using RentaFacil.API.Services.Interfaces;

namespace RentaFacil.API.Services;

/// <summary>
/// Implementación de <see cref="IDataChangeNotifier"/> sobre SignalR.
/// Emite el evento "CambioDatos" a todos los clientes conectados.
/// </summary>
public class DataChangeNotifier : IDataChangeNotifier
{
    private readonly IHubContext<DatosHub> _hubContext;
    private readonly ILogger<DataChangeNotifier> _logger;

    public DataChangeNotifier(IHubContext<DatosHub> hubContext, ILogger<DataChangeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotificarCambioAsync(string entidad, int usuarioId, string accion)
    {
        // Best-effort: si falla el envío al Hub, no debe propagarse hacia el
        // Service que ya persistió el cambio con éxito.
        try
        {
            await _hubContext.Clients.Group($"usuario-{usuarioId}").SendAsync("CambioDatos", entidad, usuarioId, accion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo notificar el cambio '{Accion}' de '{Entidad}' por SignalR", accion, entidad);
        }
    }
}
