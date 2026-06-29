namespace RentaFacil.MAUI.Config;

public static class ApiConfig
{
#if WINDOWS && LOCAL
    // Escritorio Windows en Debug: la API corre en la misma máquina.
    public static readonly string BaseUrl = "http://localhost:5295";
#elif LOCAL
    // URL para emulador Android local usando la IP de loopback especial
    public static readonly string BaseUrl = "http://10.0.2.2:5295";
#else
    // URL para producción en la LAN o servidor real
    // Reemplaza esta IP si la de tu red local cambia, o pon la URL de Render/Oracle Cloud
    public static readonly string BaseUrl = "http://200.126.17.232:5295";
#endif
}
