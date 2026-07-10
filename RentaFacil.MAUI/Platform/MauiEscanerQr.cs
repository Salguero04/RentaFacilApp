using RentaFacil.UI.Abstractions;

namespace RentaFacil.MAUI.Platform;

/// <summary>
/// Implementación de <see cref="IEscanerQr"/> para MAUI: pide permiso de cámara y
/// abre <see cref="PaginaEscanerQr"/> como página modal para escanear con ZXing.
/// </summary>
public class MauiEscanerQr : IEscanerQr
{
    public bool EstaSoportado => true;

    public async Task<string?> EscanearAsync()
    {
        var estadoPermiso = await Permissions.RequestAsync<Permissions.Camera>();
        if (estadoPermiso != PermissionStatus.Granted)
        {
            return null;
        }

        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation is null)
        {
            return null;
        }

        var pagina = new PaginaEscanerQr();
        await navigation.PushModalAsync(pagina);

        return await pagina.ResultadoTask;
    }
}
