using Microsoft.Maui.Storage;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.MAUI.Platform;

/// <summary>Implementación de <see cref="ITokenStore"/> para MAUI usando <c>SecureStorage</c>.</summary>
public class MauiTokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string RolKey = "auth_rol";

    public Task<string?> GetTokenAsync() => SecureStorage.GetAsync(TokenKey);

    public Task SetTokenAsync(string token) => SecureStorage.SetAsync(TokenKey, token);

    public Task<string?> GetRolAsync() => SecureStorage.GetAsync(RolKey);

    public Task SetRolAsync(string rol) => SecureStorage.SetAsync(RolKey, rol);

    public Task ClearAsync()
    {
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(RolKey);
        return Task.CompletedTask;
    }
}
