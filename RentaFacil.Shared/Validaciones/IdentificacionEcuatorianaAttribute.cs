using System.ComponentModel.DataAnnotations;

namespace RentaFacil.Shared.Validaciones;

public class IdentificacionEcuatorianaAttribute : ValidationAttribute
{
    public IdentificacionEcuatorianaAttribute()
    {
        ErrorMessage = "La identificación no es una cédula o RUC ecuatoriano válido.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string identificacion || string.IsNullOrWhiteSpace(identificacion))
        {
            return false;
        }

        if (!identificacion.All(char.IsDigit))
        {
            return false;
        }

        return identificacion.Length switch
        {
            10 => EsCedulaValida(identificacion),
            13 => EsRucValido(identificacion),
            _ => false
        };
    }

    private static bool EsCedulaValida(string cedula)
    {
        var provincia = int.Parse(cedula[..2]);
        var tercerDigito = cedula[2] - '0';
        if (provincia < 1 || provincia > 24) return false;
        if (tercerDigito is < 0 or > 5) return false;

        return ValidarModulo10(cedula[..9]) == cedula[9] - '0';
    }

    private static bool EsRucValido(string ruc)
    {
        var provincia = int.Parse(ruc[..2]);
        if (provincia < 1 || provincia > 24) return false;

        var tercerDigito = ruc[2] - '0';
        var sufijo = ruc[10..];

        if (tercerDigito is >= 0 and <= 5)
        {
            if (sufijo == "000") return false;
            return ValidarModulo10(ruc[..9]) == ruc[9] - '0';
        }

        if (tercerDigito == 9)
        {
            if (sufijo == "000") return false;
            return ValidarModulo11Sociedad(ruc[..9]) == ruc[9] - '0';
        }

        return false;
    }

    private static int ValidarModulo10(string nueveDigitos)
    {
        int[] coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            var producto = (nueveDigitos[i] - '0') * coeficientes[i];
            suma += producto >= 10 ? producto - 9 : producto;
        }
        var residuo = suma % 10;
        return residuo == 0 ? 0 : 10 - residuo;
    }

    private static int? ValidarModulo11Sociedad(string nueveDigitos)
    {
        int[] coeficientes = [4, 3, 2, 7, 6, 5, 4, 3, 2];
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            suma += (nueveDigitos[i] - '0') * coeficientes[i];
        }
        var residuo = suma % 11;
        var digitoVerificador = residuo == 0 ? 0 : 11 - residuo;
        return digitoVerificador == 11 ? null : digitoVerificador;
    }
}
