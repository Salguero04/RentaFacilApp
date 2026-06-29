using Microsoft.Extensions.Logging;
using RentaFacil.MAUI.Platform;
using RentaFacil.UI.Abstractions;
using RentaFacil.UI.Services;

namespace RentaFacil.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddLocalization(options => options.ResourcesPath = "Globalization/Resources");

		// API Client Configuration usando ApiConfig centralizado
		var apiBaseUrl = RentaFacil.MAUI.Config.ApiConfig.BaseUrl;

		// Implementaciones de plataforma de las abstracciones de RentaFacil.UI
		builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
		builder.Services.AddSingleton<IDispositivoServicio, MauiDispositivoServicio>();

		// HttpClient (con el Bearer token vía AuthHeaderHandler que lee del ITokenStore)
		builder.Services.AddScoped(sp =>
		{
#if DEBUG
			var innerHandler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};
#else
			var innerHandler = new HttpClientHandler();
#endif
			return new HttpClient(new AuthHeaderHandler(sp.GetRequiredService<ITokenStore>(), innerHandler))
			{
				BaseAddress = new Uri(apiBaseUrl),
				Timeout = TimeSpan.FromSeconds(20)
			};
		});

		builder.Services.AddScoped<AuthService>();
		builder.Services.AddScoped<ApiClient>();

		return builder.Build();
	}
}
