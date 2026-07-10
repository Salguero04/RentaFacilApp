using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace RentaFacil.MAUI.Platform;

/// <summary>
/// Pantalla modal nativa que muestra la cámara y detecta códigos QR con ZXing.
/// Resuelve <see cref="ResultadoTask"/> con el valor detectado, o con <c>null</c>
/// si el usuario cancela.
/// </summary>
public class PaginaEscanerQr : ContentPage
{
    private readonly TaskCompletionSource<string?> _resultado = new();
    private readonly CameraBarcodeReaderView _lectorCamara;

    public Task<string?> ResultadoTask => _resultado.Task;

    public PaginaEscanerQr()
    {
        Title = "Escanear código QR";

        _lectorCamara = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.QrCode,
                AutoRotate = true,
                Multiple = false
            }
        };
        _lectorCamara.BarcodesDetected += OnBarcodesDetected;

        var botonCancelar = new Button
        {
            Text = "Cancelar",
            Margin = new Thickness(16),
            VerticalOptions = LayoutOptions.End
        };
        botonCancelar.Clicked += (_, _) => Finalizar(null);

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                _lectorCamara,
                botonCancelar
            }
        };
        Grid.SetRow(botonCancelar, 1);
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var valor = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return;
        }

        Finalizar(valor);
    }

    private void Finalizar(string? valor)
    {
        _lectorCamara.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _resultado.TrySetResult(valor);
            await Navigation.PopModalAsync();
        });
    }
}
