namespace RentaFacil.UI.Abstractions;

/// <summary>
/// Abstracción de plataforma para obtener un ID token de Google.
/// Cada host registra su implementación; hoy ambos usan la no-soportada.
/// </summary>
public interface IProveedorGoogle
{
    bool EstaSoportado { get; }
    Task<string?> ObtenerIdTokenAsync(); // null si cancelado o falló
}
