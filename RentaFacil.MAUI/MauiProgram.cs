using Microsoft.Extensions.Logging;
using RentaFacil.MAUI.Services;

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

		builder.Services.AddSingleton<AuthService>();

#if DEBUG
		var innerHandler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};
#else
		var innerHandler = new HttpClientHandler();
#endif

		builder.Services.AddScoped(sp => new HttpClient(new AuthHeaderHandler(sp.GetRequiredService<AuthService>(), innerHandler))
		{
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});

		builder.Services.AddScoped<ApiClient>();

		return builder.Build();
	}
}
