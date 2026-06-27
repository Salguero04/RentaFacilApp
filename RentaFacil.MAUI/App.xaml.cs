using System.Globalization;

namespace RentaFacil.MAUI;

public partial class App : Application
{
	public App()
	{
		// Globalización — fija la cultura de UI al arrancar. En el futuro puede
		// venir de Preferences si el usuario elige idioma (no implementado todavía).
		CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-EC");
		CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-EC");

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "RentaFacil.MAUI" };
	}
}
