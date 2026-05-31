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

        _repositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(inquilinos);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().NombreCompleto.Should().Be("Juan Perez");
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInquilinoDto()
    {
        // Arrange
        var dto = new CrearInquilinoDto("Carlos Lopez", "789123", "555-1234", null, 1);
        var entity = new Inquilino
        {
            Id = 3,
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            UsuarioId = dto.UsuarioId
        };

        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Inquilino>())).ReturnsAsync(entity);

        // Act
        var result = await _service.CrearAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(3);
        result.NombreCompleto.Should().Be("Carlos Lopez");
    }
}
