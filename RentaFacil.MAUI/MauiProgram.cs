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

		// API Client Configuration (Using HTTP port 5295 from launchSettings.json)
		var apiBaseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5295" : "http://localhost:5295";
		
#if DEBUG
		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};
		builder.Services.AddScoped(sp => new HttpClient(handler) 
		{ 
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});
#else
		builder.Services.AddScoped(sp => new HttpClient 
		{ 
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});
#endif

		builder.Services.AddScoped<ApiClient>();
		builder.Services.AddSingleton<AuthService>();

		return builder.Build();
	}
}
