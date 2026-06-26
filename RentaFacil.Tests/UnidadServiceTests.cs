using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class UnidadServiceTests
{
    private readonly Mock<IUnidadRepository> _repositoryMock;
    private readonly Mock<IInmuebleRepository> _inmuebleRepositoryMock;
    private readonly UnidadService _service;

    public UnidadServiceTests()
    {
        _repositoryMock = new Mock<IUnidadRepository>();
        _inmuebleRepositoryMock = new Mock<IInmuebleRepository>();
        _service = new UnidadService(_repositoryMock.Object, _inmuebleRepositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_ConInmuebleDelMismoUsuario_CreaLaUnidad()
    {
        var dto = new CrearUnidadDto("Apt 1A", 500, 10);
        _inmuebleRepositoryMock.Setup(r => r.GetByIdAsync(10, 1)).ReturnsAsync(new Inmueble { Id = 10, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Unidad>())).ReturnsAsync((Unidad u) => u);

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Apt 1A");
    }

    [Fact]
    public async Task CrearAsync_ConInmuebleDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearUnidadDto("Apt 1A", 500, 10);
        _inmuebleRepositoryMock.Setup(r => r.GetByIdAsync(10, 99)).ReturnsAsync((Inmueble?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Unidad>()), Times.Never);
    }
}
