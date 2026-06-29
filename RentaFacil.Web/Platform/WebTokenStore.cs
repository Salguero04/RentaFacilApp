using Microsoft.JSInterop;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.Web.Platform;

/// <summary>Implementación de <see cref="ITokenStore"/> para web usando <c>localStorage</c> del navegador.</summary>
public class WebTokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string RolKey = "auth_rol";
    private readonly IJSRuntime _js;

    public WebTokenStore(IJSRuntime js) => _js = js;

    public async Task<string?> GetTokenAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    public async Task SetTokenAsync(string token) => await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

    public async Task<string?> GetRolAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", RolKey);

    public async Task SetRolAsync(string rol) => await _js.InvokeVoidAsync("localStorage.setItem", RolKey, rol);

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RolKey);
    }
}
