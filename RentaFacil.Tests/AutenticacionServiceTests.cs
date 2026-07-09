using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class AutenticacionServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IValidadorTokenGoogle> _validadorGoogleMock;
    private readonly AutenticacionService _service;

    public AutenticacionServiceTests()
    {
        _repositoryMock = new Mock<IUsuarioRepository>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("clave-de-prueba-suficientemente-larga-1234567890");
        _configMock.Setup(c => c["Google:PermitirRegistro"]).Returns("false");
        _validadorGoogleMock = new Mock<IValidadorTokenGoogle>();
        _service = new AutenticacionService(_repositoryMock.Object, _configMock.Object, _validadorGoogleMock.Object);
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

    [Fact]
    public async Task LoginGoogleAsync_SinConfiguracion_DevuelveNoConfigurado()
    {
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(false);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-cualquiera"));

        resultado.Resultado.Should().BeNull();
        resultado.Error.Should().Be(ErrorLoginGoogle.NoConfigurado);
        _validadorGoogleMock.Verify(v => v.ValidarAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginGoogleAsync_TokenInvalido_DevuelveTokenInvalido()
    {
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-invalido")).ReturnsAsync((GoogleTokenInfo?)null);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-invalido"));

        resultado.Resultado.Should().BeNull();
        resultado.Error.Should().Be(ErrorLoginGoogle.TokenInvalido);
    }

    [Fact]
    public async Task LoginGoogleAsync_UsuarioConGoogleIdActivo_DevuelveToken()
    {
        var info = new GoogleTokenInfo("google-123", "dueno@gmail.com", "Dueño");
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-valido")).ReturnsAsync(info);

        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno@gmail.com", GoogleId = "google-123", Rol = AppRoles.Propietario, Activo = true };
        _repositoryMock.Setup(r => r.GetByGoogleIdAsync("google-123")).ReturnsAsync(usuario);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-valido"));

        resultado.Resultado.Should().NotBeNull();
        resultado.Resultado!.NombreUsuario.Should().Be("dueno@gmail.com");
        resultado.Error.Should().BeNull();
    }

    [Fact]
    public async Task LoginGoogleAsync_UsuarioConGoogleIdInactivo_DevuelveCredencialesInvalidas()
    {
        var info = new GoogleTokenInfo("google-123", "dueno@gmail.com", "Dueño");
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-valido")).ReturnsAsync(info);

        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno@gmail.com", GoogleId = "google-123", Rol = AppRoles.Propietario, Activo = false };
        _repositoryMock.Setup(r => r.GetByGoogleIdAsync("google-123")).ReturnsAsync(usuario);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-valido"));

        resultado.Resultado.Should().BeNull();
        resultado.Error.Should().Be(ErrorLoginGoogle.CredencialesInvalidas);
    }

    [Fact]
    public async Task LoginGoogleAsync_EmailCoincidente_VinculaGoogleIdYDevuelveToken()
    {
        var info = new GoogleTokenInfo("google-456", "dueno@gmail.com", "Dueño");
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-valido")).ReturnsAsync(info);

        _repositoryMock.Setup(r => r.GetByGoogleIdAsync("google-456")).ReturnsAsync((Usuario?)null);
        var usuario = new Usuario { Id = 2, NombreUsuario = "dueno", Email = "dueno@gmail.com", Rol = AppRoles.Propietario, Activo = true };
        _repositoryMock.Setup(r => r.GetByEmailAsync("dueno@gmail.com")).ReturnsAsync(usuario);

        Usuario? actualizado = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => actualizado = u)
            .Returns(Task.CompletedTask);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-valido"));

        resultado.Resultado.Should().NotBeNull();
        resultado.Error.Should().BeNull();
        actualizado.Should().NotBeNull();
        actualizado!.GoogleId.Should().Be("google-456");
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Usuario>(u => u.GoogleId == "google-456")), Times.Once);
    }

    [Fact]
    public async Task LoginGoogleAsync_SinMatchYRegistroNoPermitido_DevuelveRegistroNoPermitido()
    {
        var info = new GoogleTokenInfo("google-789", "nuevo@gmail.com", "Nuevo");
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-valido")).ReturnsAsync(info);

        _repositoryMock.Setup(r => r.GetByGoogleIdAsync("google-789")).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.GetByEmailAsync("nuevo@gmail.com")).ReturnsAsync((Usuario?)null);
        _configMock.Setup(c => c["Google:PermitirRegistro"]).Returns("false");

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-valido"));

        resultado.Resultado.Should().BeNull();
        resultado.Error.Should().Be(ErrorLoginGoogle.RegistroNoPermitido);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginGoogleAsync_SinMatchYRegistroPermitido_CreaUsuarioYDevuelveToken()
    {
        var info = new GoogleTokenInfo("google-999", "nuevo@gmail.com", "Nuevo");
        _validadorGoogleMock.Setup(v => v.EstaConfigurado).Returns(true);
        _validadorGoogleMock.Setup(v => v.ValidarAsync("token-valido")).ReturnsAsync(info);

        _repositoryMock.Setup(r => r.GetByGoogleIdAsync("google-999")).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.GetByEmailAsync("nuevo@gmail.com")).ReturnsAsync((Usuario?)null);
        _configMock.Setup(c => c["Google:PermitirRegistro"]).Returns("true");

        Usuario? creado = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => creado = u)
            .ReturnsAsync((Usuario u) => u);

        var resultado = await _service.LoginGoogleAsync(new LoginGoogleDto("token-valido"));

        resultado.Resultado.Should().NotBeNull();
        resultado.Error.Should().BeNull();
        creado.Should().NotBeNull();
        creado!.PasswordHash.Should().BeNull();
        creado.Rol.Should().Be(AppRoles.Propietario);
        creado.Activo.Should().BeTrue();
        creado.GoogleId.Should().Be("google-999");
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Usuario>(u => u.PasswordHash == null && u.Rol == AppRoles.Propietario && u.Activo)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UsuarioSinPassword_DevuelveNull()
    {
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = null, Rol = AppRoles.Administrador, Activo = true };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "cualquier-clave"));

        resultado.Should().BeNull();
    }
}
