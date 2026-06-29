namespace RentaFacil.UI.Abstractions;

/// <summary>
/// Abstrae el almacenamiento del token JWT y el rol de la sesión.
/// Cada host la implementa con su mecanismo nativo:
/// MAUI → <c>SecureStorage</c>; Web → <c>localStorage</c> del navegador.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task<string?> GetRolAsync();
    Task SetRolAsync(string rol);
    Task ClearAsync();
}
