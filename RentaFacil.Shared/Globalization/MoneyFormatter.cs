using System.Globalization;

namespace RentaFacil.Shared.Globalization;

public static class MoneyFormatter
{
    private static readonly CultureInfo EcuadorCulture = new("es-EC");

    public static string Mostrar(decimal monto, string? cultura = null)
    {
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        return monto.ToString("C2", ci);
    }

    public static string MostrarNumero(decimal monto, string? cultura = null)
    {
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        return monto.ToString("N2", ci);
    }

    public static decimal? Parsear(string input, string? cultura = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        if (decimal.TryParse(input, NumberStyles.Number, ci, out var result))
            return result;
        return null;
    }
}
