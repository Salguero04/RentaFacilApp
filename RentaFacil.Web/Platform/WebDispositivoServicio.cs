using Microsoft.JSInterop;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.Web.Platform;

/// <summary>
/// Implementación de <see cref="IDispositivoServicio"/> para web:
/// abre enlaces con <c>window.open</c> y "comparte" archivos disparando una descarga del navegador.
/// </summary>
public class WebDispositivoServicio : IDispositivoServicio
{
    private readonly IJSRuntime _js;

    public WebDispositivoServicio(IJSRuntime js) => _js = js;

    public async Task AbrirEnlaceAsync(string url) => await _js.InvokeVoidAsync("open", url, "_blank");

    public async Task GuardarArchivoAsync(byte[] contenido, string nombreArchivo, string titulo)
    {
        // En el navegador no hay "hoja de compartir": descargamos el archivo.
        var base64 = Convert.ToBase64String(contenido);
        await _js.InvokeVoidAsync("rentaFacilDescargarArchivo", nombreArchivo, base64);
    }
}
