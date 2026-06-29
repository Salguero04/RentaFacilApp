namespace RentaFacil.UI.Abstractions;

/// <summary>
/// Abstrae acciones que dependen del dispositivo/plataforma.
/// MAUI → <c>Launcher</c>/<c>Share</c>; Web → <c>window.open</c>/descarga de archivo.
/// </summary>
public interface IDispositivoServicio
{
    /// <summary>Abre un enlace externo (p. ej. un deep link de WhatsApp).</summary>
    Task AbrirEnlaceAsync(string url);

    /// <summary>
    /// Guarda o comparte un archivo ya descargado (p. ej. el PDF de un recibo).
    /// En móvil abre la hoja de compartir; en web dispara la descarga.
    /// </summary>
    Task GuardarArchivoAsync(byte[] contenido, string nombreArchivo, string titulo);
}
