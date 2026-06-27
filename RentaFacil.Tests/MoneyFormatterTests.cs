using System.Globalization;
using FluentAssertions;
using RentaFacil.Shared.Globalization;

namespace RentaFacil.Tests;

public class MoneyFormatterTests
{
    [Theory]
    [InlineData(1500.50, "es-EC", "$1.500,50")]
    [InlineData(1500.50, "en-US", "$1,500.50")]
    [InlineData(0, "es-EC", "$0,00")]
    [InlineData(250.00, "es-EC", "$250,00")]
    public void Mostrar_DevuelveFormatoSegunCultura(decimal monto, string cultura, string esperado)
        => MoneyFormatter.Mostrar(monto, cultura).Should().Be(esperado);

    [Theory]
    [InlineData("1500,50", "es-EC", 1500.50)]
    [InlineData("1.500,50", "es-EC", 1500.50)]
    [InlineData("1500.50", "en-US", 1500.50)]
    [InlineData("250", "es-EC", 250.00)]
    [InlineData("abc", "es-EC", null)]
    [InlineData("", "es-EC", null)]
    [InlineData("  ", "es-EC", null)]
    public void Parsear_DevuelveDecimalONull(string input, string cultura, object? esperado)
    {
        // xUnit InlineData no admite decimal? como parámetro directo: el literal numérico
        // llega boxeado como double y la conversión double -> decimal? falla por reflexión.
        // Se recibe como object y se convierte acá adentro.
        decimal? esperadoDecimal = esperado is null ? null : Convert.ToDecimal(esperado);
        MoneyFormatter.Parsear(input, cultura).Should().Be(esperadoDecimal);
    }

    [Fact]
    public void InvariantCulture_UsaPuntoComoDecimal()
        => (1500.50m).ToString(CultureInfo.InvariantCulture).Should().Be("1500.50");

    [Fact]
    public void JsonDecimal_SerializaConPuntoSinImportarCultura()
    {
        var dto = new { Monto = 1500.50m };
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        json.Should().Contain("1500.5");
    }
}
