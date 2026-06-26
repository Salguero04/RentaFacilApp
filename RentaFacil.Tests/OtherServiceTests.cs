using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class InmuebleServiceTests
{
    private readonly Mock<IInmuebleRepository> _repositoryMock;
    private readonly InmuebleService _service;

    public InmuebleServiceTests()
    {
        _repositoryMock = new Mock<IInmuebleRepository>();
        _service = new InmuebleService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnInmuebles()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Inmueble> { new Inmueble { Id = 1, Nombre = "Casa 1", Direccion = "Calle 1" } });
        var result = await _service.GetAllAsync(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInmueble()
    {
        var dto = new CrearInmuebleDto("Edificio A", "Avenida 2", TipoInmueble.Multiple, 0);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Inmueble>())).ReturnsAsync(new Inmueble { Id = 2, Nombre = dto.Nombre, Tipo = dto.Tipo });
        var result = await _service.CrearAsync(dto, 1);
        result.Nombre.Should().Be("Edificio A");
        result.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ConUsuarioIdDeOtroUsuario_NoDevuelveElInmueble()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync((Inmueble?)null);
        var result = await _service.GetByIdAsync(1, 99);
        result.Should().BeNull();
    }
}

public class ContratoServiceTests
{
    private readonly Mock<IContratoRepository> _repositoryMock;
    private readonly Mock<IInquilinoRepository> _inquilinoRepositoryMock;
    private readonly Mock<IUnidadRepository> _unidadRepositoryMock;
    private readonly ContratoService _service;

    public ContratoServiceTests()
    {
        _repositoryMock = new Mock<IContratoRepository>();
        _inquilinoRepositoryMock = new Mock<IInquilinoRepository>();
        _unidadRepositoryMock = new Mock<IUnidadRepository>();
        _service = new ContratoService(_repositoryMock.Object, _inquilinoRepositoryMock.Object, _unidadRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnContratos()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Contrato> { new Contrato { Id = 1, Monto = 500 } });
        var result = await _service.GetAllAsync(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CrearAsync_ConInquilinoYUnidadDelMismoUsuario_CreaElContrato()
    {
        var dto = new CrearContratoDto(1, 1, 500, 500, 12, 5, DateTime.Now, null);
        _inquilinoRepositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new Inquilino { Id = 1, UsuarioId = 1 });
        _unidadRepositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new Unidad { Id = 1, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contrato>())).ReturnsAsync((Contrato c) => c);

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CrearAsync_ConUnidadDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearContratoDto(1, 1, 500, 500, 12, 5, DateTime.Now, null);
        _inquilinoRepositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync(new Inquilino { Id = 1, UsuarioId = 99 });
        _unidadRepositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync((Unidad?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contrato>()), Times.Never);
    }
}

public class PagoServiceTests
{
    private readonly Mock<IPagoRepository> _repositoryMock;
    private readonly Mock<IContratoRepository> _contratoRepositoryMock;
    private readonly PagoService _service;

    public PagoServiceTests()
    {
        _repositoryMock = new Mock<IPagoRepository>();
        _contratoRepositoryMock = new Mock<IContratoRepository>();
        _service = new PagoService(_repositoryMock.Object, _contratoRepositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_ShouldCalculateCompletado()
    {
        var dto = new CrearPagoDto(1, 500, 500, 0, DateTime.Now, "MAY-26");
        _contratoRepositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new Contrato { Id = 1, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Pago>())).ReturnsAsync(new Pago { Id = 1, TotalMonto = 500, ACuenta = 500, Completado = true, Periodo = "MAY-26" });

        var result = await _service.CrearAsync(dto, 1);

        result!.Completado.Should().BeTrue();
    }

    [Fact]
    public async Task CrearAsync_ConContratoDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearPagoDto(1, 500, 500, 0, DateTime.Now, "MAY-26");
        _contratoRepositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync((Contrato?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Pago>()), Times.Never);
    }
}
