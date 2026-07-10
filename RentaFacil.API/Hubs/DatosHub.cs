using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RentaFacil.API.Hubs;

/// <summary>
/// Hub de tiempo real. Los clientes (MAUI/Web) solo escuchan el evento
/// "CambioDatos" para refrescarse cuando otro cliente modifica datos;
/// no invocan métodos del servidor, por eso el cuerpo va vacío.
/// Requiere autenticación (el token JWT llega por query string, ver Program.cs).
/// </summary>
[Authorize]
public class DatosHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Cada cliente solo escucha su propio grupo: los eventos del arrendador
        // no llegan a los inquilinos (ni a otros usuarios) y viceversa.
        var id = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(id))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario-{id}");
        await base.OnConnectedAsync();
    }
}
