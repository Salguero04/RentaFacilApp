using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.MAUI.Platform;

/// <summary>
/// Implementación de <see cref="IDispositivoServicio"/> para MAUI:
/// abre enlaces con <c>Launcher</c> y comparte archivos con la hoja nativa <c>Share</c>.
/// </summary>
public class MauiDispositivoServicio : IDispositivoServicio
{
    public Task AbrirEnlaceAsync(string url) => Launcher.OpenAsync(url);

    public async Task GuardarArchivoAsync(byte[] contenido, string nombreArchivo, string titulo)
    {
        var rutaTemporal = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
        await File.WriteAllBytesAsync(rutaTemporal, contenido);

        await Share.RequestAsync(new ShareFileRequest
        {
            Title = titulo,
            File = new ShareFile(rutaTemporal)
        });
    }
}
