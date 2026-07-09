namespace RentaFacil.API.Services.Interfaces;

/// <summary>
/// Datos extraídos de un ID token de Google ya validado (claim "sub", email, nombre y si el
/// email está verificado por Google). <see cref="EmailVerified"/> es obligatorio para vincular
/// o crear una cuenta por email: sin él, cualquiera con un email no verificado podría apropiarse
/// de una cuenta existente.
/// </summary>
public record GoogleTokenInfo(string GoogleId, string Email, string? Nombre, bool EmailVerified);

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
