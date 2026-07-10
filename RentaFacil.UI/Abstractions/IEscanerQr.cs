namespace RentaFacil.UI.Abstractions;

/// <summary>
/// Escaneo de códigos QR con la cámara. MAUI → ZXing; Web → no soportado (código manual).
/// </summary>
public interface IEscanerQr
{
    bool EstaSoportado { get; }
    Task<string?> EscanearAsync();   // null si el usuario cancela o no hay permiso
}
