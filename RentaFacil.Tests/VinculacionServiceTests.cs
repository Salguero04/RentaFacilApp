using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class VinculacionServiceTests
{
    private readonly Mock<ICodigoVinculacionRepository> _codigoRepoMock;
    private readonly Mock<IContratoRepository> _contratoRepoMock;
    private readonly Mock<IInquilinoRepository> _inquilinoRepoMock;
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IAutenticacionService> _autenticacionServiceMock;
    private readonly VinculacionService _service;

    public VinculacionServiceTests()
    {
        _codigoRepoMock = new Mock<ICodigoVinculacionRepository>();
        _contratoRepoMock = new Mock<IContratoRepository>();
        _inquilinoRepoMock = new Mock<IInquilinoRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _autenticacionServiceMock = new Mock<IAutenticacionService>();
        _service = new VinculacionService(
            _codigoRepoMock.Object,
            _contratoRepoMock.Object,
            _inquilinoRepoMock.Object,
            _usuarioRepoMock.Object,
            _autenticacionServiceMock.Object);
    }

    [Fact]
    public async Task GenerarCodigo_ContratoPropio_Genera8CharsSinAmbiguosYExpira7Dias()
    {
        var contrato = new Contrato { Id = 10, InquilinoId = 5, UsuarioId = 1, Activo = true };
        _contratoRepoMock.Setup(r => r.GetByIdAsync(10, 1)).ReturnsAsync(contrato);

        CodigoVinculacion? guardado = null;
        _codigoRepoMock.Setup(r => r.AddAsync(It.IsAny<CodigoVinculacion>()))
            .Callback<CodigoVinculacion>(c => guardado = c)
            .ReturnsAsync((CodigoVinculacion c) => c);

        var antes = DateTime.UtcNow;
        var resultado = await _service.GenerarCodigoAsync(10, 1);
        var despues = DateTime.UtcNow;

        resultado.Should().NotBeNull();
        resultado!.Codigo.Should().HaveLength(8);
        resultado.Codigo.Should().MatchRegex("^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{8}$");
        resultado.FechaExpiracion.Should().BeCloseTo(antes.AddDays(7), TimeSpan.FromMinutes(1));
        resultado.FechaExpiracion.Should().BeOnOrBefore(despues.AddDays(7).AddMinutes(1));

        guardado.Should().NotBeNull();
        guardado!.ContratoId.Should().Be(10);
        guardado.InquilinoId.Should().Be(5);
        guardado.UsuarioId.Should().Be(1);
        guardado.UsadoEn.Should().BeNull();
    }

    [Fact]
    public async Task GenerarCodigo_ContratoAjeno_DevuelveNull()
    {
        _contratoRepoMock.Setup(r => r.GetByIdAsync(10, 1)).ReturnsAsync((Contrato?)null);

        var resultado = await _service.GenerarCodigoAsync(10, 1);

        resultado.Should().BeNull();
        _codigoRepoMock.Verify(r => r.AddAsync(It.IsAny<CodigoVinculacion>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarInquilino_CodigoInexistenteOExpiradoOUsado_DevuelveError()
    {
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync((CodigoVinculacion?)null);

        var dto = new RegistrarInquilinoDto("ABCD1234", "nuevoinquilino", "clave1234", null);
        var (resultado, error) = await _service.RegistrarInquilinoAsync(dto);

        resultado.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
        _usuarioRepoMock.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarInquilino_NombreUsuarioTomado_DevuelveError()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("tomado")).ReturnsAsync(new Usuario { Id = 99, NombreUsuario = "tomado", Rol = AppRoles.Inquilino, Activo = true });

        var dto = new RegistrarInquilinoDto("ABCD1234", "tomado", "clave1234", null);
        var (resultado, error) = await _service.RegistrarInquilinoAsync(dto);

        resultado.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
        _usuarioRepoMock.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarInquilino_CodigoVigente_CreaCuentaVinculaYDevuelveToken()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("nuevoinquilino")).ReturnsAsync((Usuario?)null);
        _codigoRepoMock.Setup(r => r.ReclamarAsync(1)).ReturnsAsync(true);

        var inquilino = new Inquilino { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = null };
        _inquilinoRepoMock.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(inquilino);

        Usuario? usuarioCreado = null;
        _usuarioRepoMock.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => { u.Id = 42; usuarioCreado = u; })
            .ReturnsAsync((Usuario u) => u);

        Inquilino? inquilinoActualizado = null;
        _inquilinoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Inquilino>()))
            .Callback<Inquilino>(i => inquilinoActualizado = i)
            .Returns(Task.CompletedTask);

        var tokenEsperado = new LoginResultDto("token-jwt", "nuevoinquilino", AppRoles.Inquilino, DateTime.UtcNow.AddHours(8));
        _autenticacionServiceMock.Setup(s => s.EmitirToken(It.IsAny<Usuario>())).Returns(tokenEsperado);

        var dto = new RegistrarInquilinoDto("ABCD1234", "nuevoinquilino", "clave1234", "inquilino@correo.com");
        var (resultado, error) = await _service.RegistrarInquilinoAsync(dto);

        error.Should().BeNull();
        resultado.Should().NotBeNull();
        resultado!.Token.Should().Be("token-jwt");

        usuarioCreado.Should().NotBeNull();
        usuarioCreado!.Rol.Should().Be(AppRoles.Inquilino);
        usuarioCreado.Email.Should().Be("inquilino@correo.com");
        usuarioCreado.Activo.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("clave1234", usuarioCreado.PasswordHash).Should().BeTrue();

        inquilinoActualizado.Should().NotBeNull();
        inquilinoActualizado!.UsuarioCuentaId.Should().Be(42);

        _codigoRepoMock.Verify(r => r.ReclamarAsync(1), Times.Once);
        _usuarioRepoMock.Verify(r => r.AddAsync(It.Is<Usuario>(u => u.Rol == AppRoles.Inquilino && u.Email == "inquilino@correo.com")), Times.Once);
        _inquilinoRepoMock.Verify(r => r.UpdateAsync(It.Is<Inquilino>(i => i.UsuarioCuentaId == 42)), Times.Once);
        _codigoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CodigoVinculacion>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarInquilino_CodigoReclamadoPorOtroRequest_DevuelveError()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("nuevoinquilino")).ReturnsAsync((Usuario?)null);
        _codigoRepoMock.Setup(r => r.ReclamarAsync(1)).ReturnsAsync(false);

        var inquilino = new Inquilino { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = null };
        _inquilinoRepoMock.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(inquilino);

        var dto = new RegistrarInquilinoDto("ABCD1234", "nuevoinquilino", "clave1234", "inquilino@correo.com");
        var (resultado, error) = await _service.RegistrarInquilinoAsync(dto);

        resultado.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
        _codigoRepoMock.Verify(r => r.ReclamarAsync(1), Times.Once);
        _usuarioRepoMock.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
        _inquilinoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Inquilino>()), Times.Never);
    }

    [Fact]
    public async Task VincularCuentaExistente_CodigoVigente_SeteaUsuarioCuentaIdYMarcaUsado()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _codigoRepoMock.Setup(r => r.ReclamarAsync(1)).ReturnsAsync(true);

        var inquilino = new Inquilino { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = null };
        _inquilinoRepoMock.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(inquilino);

        Inquilino? inquilinoActualizado = null;
        _inquilinoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Inquilino>()))
            .Callback<Inquilino>(i => inquilinoActualizado = i)
            .Returns(Task.CompletedTask);

        var exito = await _service.VincularCuentaExistenteAsync("ABCD1234", 77);

        exito.Should().BeTrue();
        inquilinoActualizado.Should().NotBeNull();
        inquilinoActualizado!.UsuarioCuentaId.Should().Be(77);
        _codigoRepoMock.Verify(r => r.ReclamarAsync(1), Times.Once);
        _codigoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CodigoVinculacion>()), Times.Never);
    }

    [Fact]
    public async Task VincularCuentaExistente_CodigoReclamadoPorOtroRequest_DevuelveFalse()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _codigoRepoMock.Setup(r => r.ReclamarAsync(1)).ReturnsAsync(false);

        var inquilino = new Inquilino { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = null };
        _inquilinoRepoMock.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(inquilino);

        var exito = await _service.VincularCuentaExistenteAsync("ABCD1234", 77);

        exito.Should().BeFalse();
        _codigoRepoMock.Verify(r => r.ReclamarAsync(1), Times.Once);
        _inquilinoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Inquilino>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarInquilino_InquilinoYaVinculadoAOtraCuenta_DevuelveError()
    {
        var codigo = new CodigoVinculacion { Id = 1, Codigo = "ABCD1234", ContratoId = 10, InquilinoId = 5, UsuarioId = 1, FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddDays(7) };
        _codigoRepoMock.Setup(r => r.GetVigenteAsync("ABCD1234")).ReturnsAsync(codigo);
        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("nuevoinquilino")).ReturnsAsync((Usuario?)null);

        var inquilino = new Inquilino { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 999 };
        _inquilinoRepoMock.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(inquilino);

        var dto = new RegistrarInquilinoDto("ABCD1234", "nuevoinquilino", "clave1234", null);
        var (resultado, error) = await _service.RegistrarInquilinoAsync(dto);

        resultado.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
        _usuarioRepoMock.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
        _inquilinoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Inquilino>()), Times.Never);
    }
}
