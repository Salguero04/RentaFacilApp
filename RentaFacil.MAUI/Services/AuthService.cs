using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using RentaFacil.MAUI.Config;
using RentaFacil.Shared.Models;

namespace RentaFacil.MAUI.Services;

public class AuthService
{
    private const string TokenKey = "auth_token";
    private const string RolKey = "auth_rol";
    private readonly HttpClient _http;

    public bool IsAuthenticated { get; private set; }
    public string? Rol { get; private set; }

    public event Action? OnAuthStateChanged;

    public AuthService()
    {
#if DEBUG
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
#else
        _http = new HttpClient { BaseAddress = new Uri(ApiConfig.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
#endif
    }

    public async Task InicializarAsync()
    {
        var token = await SecureStorage.GetAsync(TokenKey);
        IsAuthenticated = !string.IsNullOrEmpty(token);
        Rol = await SecureStorage.GetAsync(RolKey);
    }

    public async Task<bool> LoginAsync(string nombreUsuario, string password)
    {
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/auth/login", new LoginDto(nombreUsuario, password));
            if (!respuesta.IsSuccessStatusCode) return false;

            var resultado = await respuesta.Content.ReadFromJsonAsync<LoginResultDto>();
            if (resultado == null) return false;

            SecureStorage.SetAsync(TokenKey, resultado.Token).Wait();
            SecureStorage.SetAsync(RolKey, resultado.Rol).Wait();
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

    public void Logout()
    {
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(RolKey);
        IsAuthenticated = false;
        Rol = null;
        OnAuthStateChanged?.Invoke();
    }
}
