using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RentaFacil.UI.Abstractions;
using RentaFacil.UI.Services;
using RentaFacil.Web;
using RentaFacil.Web.Platform;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL de la API. En desarrollo apunta a la API local (mismo backend que usa MAUI).
// En producción, reemplazar por la URL pública del backend (Render/Oracle).
const string apiBaseUrl = "http://localhost:5295";

// Implementaciones de plataforma de las abstracciones de RentaFacil.UI (versión navegador).
builder.Services.AddScoped<ITokenStore, WebTokenStore>();
builder.Services.AddScoped<IDispositivoServicio, WebDispositivoServicio>();
builder.Services.AddScoped<IProveedorGoogle, ProveedorGoogleNoSoportado>();

// HttpClient hacia la API, con el Bearer token adjuntado por AuthHeaderHandler (lee del ITokenStore).
builder.Services.AddScoped(sp =>
    new HttpClient(new AuthHeaderHandler(sp.GetRequiredService<ITokenStore>(), new HttpClientHandler()))
    {
        BaseAddress = new Uri(apiBaseUrl),
        Timeout = TimeSpan.FromSeconds(20)
    });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClient>();

// Cliente SignalR para tiempo real. Scoped (en WASM equivale a un singleton
// por sesión de la app), usando la misma URL base que ApiClient.
builder.Services.AddScoped(sp => new SignalRClient(apiBaseUrl, sp.GetRequiredService<ITokenStore>()));

builder.Services.AddLocalization();

await builder.Build().RunAsync();
