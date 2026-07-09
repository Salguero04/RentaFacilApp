namespace RentaFacil.API.Services.Interfaces;

/// <summary>
/// Notifica a los clientes conectados por SignalR que una entidad cambió,
/// para que se refresquen sin recargar manualmente.
/// </summary>
public interface IDataChangeNotifier
{
    /// <summary>
    /// Emite el evento "CambioDatos" a todos los clientes.
    /// </summary>
    /// <param name="entidad">Nombre de la entidad afectada, ej. "Pago" o "Contrato".</param>
    /// <param name="usuarioId">Id del arrendador dueño del dato.</param>
    /// <param name="accion">"crear", "actualizar" o "eliminar".</param>
    Task NotificarCambioAsync(string entidad, int usuarioId, string accion);
}
