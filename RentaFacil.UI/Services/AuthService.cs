using System.Net.Http.Json;
using RentaFacil.Shared.Models;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.UI.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokenStore;

    public bool IsAuthenticated { get; private set; }
    public string? Rol { get; private set; }

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient http, ITokenStore tokenStore)
    {
        _http = http;
        _tokenStore = tokenStore;
    }

    public async Task InicializarAsync()
    {
        var token = await _tokenStore.GetTokenAsync();
        IsAuthenticated = !string.IsNullOrEmpty(token);
        Rol = await _tokenStore.GetRolAsync();
    }

    public async Task<bool> LoginAsync(string nombreUsuario, string password)
    {
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/auth/login", new LoginDto(nombreUsuario, password));
            if (!respuesta.IsSuccessStatusCode) return false;

            var resultado = await respuesta.Content.ReadFromJsonAsync<LoginResultDto>();
            if (resultado == null) return false;

            await _tokenStore.SetTokenAsync(resultado.Token);
            await _tokenStore.SetRolAsync(resultado.Rol);
            IsAuthenticated = true;
            Rol = resultado.Rol;
            OnAuthStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        IsAuthenticated = false;
        Rol = null;
        OnAuthStateChanged?.Invoke();
    }
}
