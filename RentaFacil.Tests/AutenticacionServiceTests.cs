using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class AutenticacionServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AutenticacionService _service;

    public AutenticacionServiceTests()
    {
        _repositoryMock = new Mock<IUsuarioRepository>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("clave-de-prueba-suficientemente-larga-1234567890");
        _service = new AutenticacionService(_repositoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveToken()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = true };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave123"));

        resultado.Should().NotBeNull();
        resultado!.NombreUsuario.Should().Be("dueno");
        resultado.Rol.Should().Be(AppRoles.Administrador);
        resultado.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ConPasswordIncorrecta_DevuelveNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = true };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave-equivocada"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("fantasma")).ReturnsAsync((Usuario?)null);

        var resultado = await _service.LoginAsync(new LoginDto("fantasma", "clave123"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInactivo_DevuelveNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = false };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave123"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task RegistrarAsync_HasheaLaPasswordAntesDeGuardar()
    {
        Usuario? guardado = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => guardado = u)
            .ReturnsAsync((Usuario u) => u);

        await _service.RegistrarAsync(new RegistrarUsuarioDto("nuevo", "clave123", AppRoles.Propietario));

        guardado.Should().NotBeNull();
        guardado!.PasswordHash.Should().NotBe("clave123");
        BCrypt.Net.BCrypt.Verify("clave123", guardado.PasswordHash).Should().BeTrue();
    }
}
