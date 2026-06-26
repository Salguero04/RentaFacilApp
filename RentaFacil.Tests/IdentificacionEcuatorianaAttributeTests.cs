using FluentAssertions;
using RentaFacil.Shared.Validaciones;

namespace RentaFacil.Tests;

public class IdentificacionEcuatorianaAttributeTests
{
    private readonly IdentificacionEcuatorianaAttribute _attribute = new();

    [Theory]
    [InlineData("1712345675")]      // cédula válida
    [InlineData("1712345675001")]   // RUC persona natural válido
    [InlineData("1791234561001")]   // RUC sociedad válido
    public void IsValid_ConIdentificacionValida_DevuelveTrue(string identificacion)
    {
        _attribute.IsValid(identificacion).Should().BeTrue();
    }

    [Theory]
    [InlineData("1712345674")]      // cédula con dígito verificador incorrecto
    [InlineData("0012345675")]      // provincia inválida (00)
    [InlineData("1762345675")]      // tercer dígito inválido para cédula (6)
    [InlineData("1712345675000")]   // RUC natural con sufijo 000
    [InlineData("1791234562001")]   // RUC sociedad con dígito verificador incorrecto
    [InlineData("1771234567001")]   // RUC con tercer dígito no soportado (7, sector público/otros)
    [InlineData("171234567A")]      // contiene una letra
    [InlineData("12345")]           // longitud inválida
    [InlineData("")]                // vacío
    public void IsValid_ConIdentificacionInvalida_DevuelveFalse(string identificacion)
    {
        _attribute.IsValid(identificacion).Should().BeFalse();
    }
}
