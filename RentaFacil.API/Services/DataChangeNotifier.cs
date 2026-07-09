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

    public DataChangeNotifier(IHubContext<DatosHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificarCambioAsync(string entidad, int usuarioId, string accion)
    {
        await _hubContext.Clients.All.SendAsync("CambioDatos", entidad, usuarioId, accion);
    }
}
