using Microsoft.AspNetCore.SignalR.Client;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.UI.Services;

/// <summary>
/// Cliente SignalR compartido por los hosts (MAUI/Web). Mantiene una única
/// conexión al Hub del backend y re-emite el evento "CambioDatos" para que
/// las páginas se refresquen cuando otro cliente modifica datos.
/// Vive a nivel DI (Singleton en MAUI, Scoped en Web); las páginas se
/// suscriben/desuscriben pero NO cierran la conexión.
/// </summary>
public class SignalRClient : IAsyncDisposable
{
    private readonly string _baseUrl;
    private readonly ITokenStore _tokenStore;
    private HubConnection? _connection;

    /// <summary>
    /// Se dispara cuando el servidor emite "CambioDatos".
    /// Argumentos: entidad ("Pago"/"Contrato"), usuarioId, acción ("crear"/"actualizar"/"eliminar").
    /// OJO: llega en un hilo de fondo — usar InvokeAsync/StateHasChanged al reaccionar.
    /// </summary>
    public event Action<string, int, string>? CambioDatos;

    public SignalRClient(string baseUrl, ITokenStore tokenStore)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Inicia la conexión al Hub. Idempotente: si ya está conectada, no hace nada.
    /// Nunca lanza: si no hay token o el servidor no responde, loguea y sigue.
    /// </summary>
    public async Task IniciarAsync()
    {
        if (_connection is not null)
        {
            // Ya se construyó la conexión; solo intentamos (re)conectar si hace falta.
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await IntentarConectarAsync();
            }
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hubs/datos", options =>
            {
                options.AccessTokenProvider = async () => await _tokenStore.GetTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, int, string>("CambioDatos", (entidad, usuarioId, accion) =>
        {
            CambioDatos?.Invoke(entidad, usuarioId, accion);
        });

        await IntentarConectarAsync();
    }

    private async Task IntentarConectarAsync()
    {
        try
        {
            await _connection!.StartAsync();
        }
        catch (Exception ex)
        {
            // No es crítico: la app funciona sin tiempo real. La reconexión
            // automática seguirá intentando si el servidor vuelve.
            Console.WriteLine($"[SignalRClient] No se pudo conectar al Hub: {ex.Message}");
        }
    }

    /// <summary>
    /// Detiene la conexión (sin destruirla). Idempotente.
    /// </summary>
    public async Task DetenerAsync()
    {
        if (_connection is not null && _connection.State != HubConnectionState.Disconnected)
        {
            try
            {
                await _connection.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalRClient] Error al detener el Hub: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        GC.SuppressFinalize(this);
    }
}
