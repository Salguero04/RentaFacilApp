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
}
