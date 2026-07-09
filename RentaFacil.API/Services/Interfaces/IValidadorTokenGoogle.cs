namespace RentaFacil.API.Services.Interfaces;

/// <summary>
/// Datos extraídos de un ID token de Google ya validado (claim "sub", email y nombre).
/// </summary>
public record GoogleTokenInfo(string GoogleId, string Email, string? Nombre);

/// <summary>
/// Valida ID tokens de Google contra la librería oficial. Sin "Google:ClientId"
/// configurado, <see cref="EstaConfigurado"/> es false y la API sigue funcionando
/// con el login usuario/contraseña de siempre.
/// </summary>
public interface IValidadorTokenGoogle
{
    bool EstaConfigurado { get; }

    Task<GoogleTokenInfo?> ValidarAsync(string idToken);
}
