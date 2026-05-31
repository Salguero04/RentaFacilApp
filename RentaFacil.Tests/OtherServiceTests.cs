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
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Inmueble> { new Inmueble { Id = 1, Nombre = "Casa 1", Direccion = "Calle 1" } });
        var result = await _service.GetAllAsync();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInmueble()
    {
        var dto = new CrearInmuebleDto("Edificio A", "Avenida 2", TipoInmueble.Multiple, 0, 1);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Inmueble>())).ReturnsAsync(new Inmueble { Id = 2, Nombre = dto.Nombre, Tipo = dto.Tipo });
        var result = await _service.CrearAsync(dto);
        result.Nombre.Should().Be("Edificio A");
        result.Id.Should().Be(2);
    }
}

public class ContratoServiceTests
{
    private readonly Mock<IContratoRepository> _repositoryMock;
    private readonly ContratoService _service;

    public ContratoServiceTests()
    {
        _repositoryMock = new Mock<IContratoRepository>();
        _service = new ContratoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnContratos()
    {
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Contrato> { new Contrato { Id = 1, Monto = 500 } });
        var result = await _service.GetAllAsync();
        result.Should().HaveCount(1);
    }
}

public class PagoServiceTests
{
    private readonly Mock<IPagoRepository> _repositoryMock;
    private readonly PagoService _service;

    public PagoServiceTests()
    {
        _repositoryMock = new Mock<IPagoRepository>();
        _service = new PagoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_ShouldCalculateCompletado()
    {
        var dto = new CrearPagoDto(1, 500, 500, 0, DateTime.Now, "MAY-26");
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Pago>())).ReturnsAsync(new Pago { Id = 1, TotalMonto = 500, ACuenta = 500, Completado = true, Periodo = "MAY-26" });
        var result = await _service.CrearAsync(dto);
        result.Completado.Should().BeTrue();
    }
}
