using System.Security.Claims;
using FluentAssertions;
using RentaFacil.API.Extensions;

namespace RentaFacil.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void ObtenerUsuarioId_ConClaimValido_DevuelveElId()
    {
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        var principal = new ClaimsPrincipal(identidad);

        var resultado = principal.ObtenerUsuarioId();

        resultado.Should().Be(42);
    }

    [Fact]
    public void ObtenerUsuarioId_SinClaim_LanzaExcepcion()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var accion = () => principal.ObtenerUsuarioId();

        accion.Should().Throw<InvalidOperationException>();
    }
}
