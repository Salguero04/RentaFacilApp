using RentaFacil.UI.Abstractions;

namespace RentaFacil.UI.Services;

/// <summary>
/// Implementación por defecto de <see cref="IEscanerQr"/> para plataformas sin
/// acceso a la cámara vía ZXing (hoy, la web). Reporta que el escaneo no está soportado.
/// </summary>
public class EscanerQrNoSoportado : IEscanerQr
{
    public bool EstaSoportado => false;

    public Task<string?> EscanearAsync() => Task.FromResult<string?>(null);
}
