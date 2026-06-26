using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class InquilinoServiceTests
{
    private readonly Mock<IInquilinoRepository> _repositoryMock;
    private readonly InquilinoService _service;

    public InquilinoServiceTests()
    {
        _repositoryMock = new Mock<IInquilinoRepository>();
        _service = new InquilinoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnListOfInquilinos()
    {
        // Arrange
        var inquilinos = new List<Inquilino>
        {
            new Inquilino { Id = 1, NombreCompleto = "Juan Perez", Identificacion = "123456", UsuarioId = 1 },
            new Inquilino { Id = 2, NombreCompleto = "Maria Gomez", Identificacion = "654321", UsuarioId = 1 }
        };

        _repositoryMock.Setup(repo => repo.GetAllAsync(1)).ReturnsAsync(inquilinos);

        // Act
        var result = await _service.GetAllAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.First().NombreCompleto.Should().Be("Juan Perez");
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInquilinoDto()
    {
        // Arrange
        var dto = new CrearInquilinoDto("Carlos Lopez", "789123", "555-1234", null);
        var entity = new Inquilino
        {
            Id = 3,
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            UsuarioId = 1
        };

        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Inquilino>())).ReturnsAsync(entity);

        // Act
        var result = await _service.CrearAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(3);
        result.NombreCompleto.Should().Be("Carlos Lopez");
    }

    [Fact]
    public async Task CrearAsync_AsignaUsuarioIdDelLlamador_NoDelDto()
    {
        // Arrange
        var dto = new CrearInquilinoDto("Carlos Lopez", "789123", "555-1234", null);
        Inquilino? capturado = null;
        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Inquilino>()))
            .Callback<Inquilino>(i => capturado = i)
            .ReturnsAsync((Inquilino i) => i);

        // Act
        await _service.CrearAsync(dto, 42);

        // Assert
        capturado.Should().NotBeNull();
        capturado!.UsuarioId.Should().Be(42);
    }

    [Fact]
    public async Task GetByIdAsync_ConUsuarioIdDeOtroUsuario_NoDevuelveElInquilino()
    {
        // Arrange: el repositorio simula el filtrado IDOR devolviendo null para un usuarioId distinto al propietario.
        _repositoryMock.Setup(repo => repo.GetByIdAsync(1, 99)).ReturnsAsync((Inquilino?)null);

        // Act
        var result = await _service.GetByIdAsync(1, 99);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(repo => repo.GetByIdAsync(1, 99), Times.Once);
    }
}
