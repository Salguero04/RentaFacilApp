using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using RentaFacil.API.Data;
using RentaFacil.API.Models;

namespace RentaFacil.Tests;

public class AuditoriaInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public AuditoriaInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    private AppDbContext CrearContexto()
    {
        var interceptor = new AuditoriaInterceptor(_httpContextAccessorMock.Object);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private void ConfigurarUsuarioAutenticado(int usuarioId)
    {
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) });
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
    }

    [Fact]
    public void Added_ConUsuarioAutenticado_SellaCreadoYModificado()
    {
        ConfigurarUsuarioAutenticado(7);
        using var context = CrearContexto();

        var inquilino = new Inquilino { NombreCompleto = "Test", Identificacion = "1", UsuarioId = 7, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        context.SaveChanges();

        inquilino.CreadoPorId.Should().Be(7);
        inquilino.FechaCreacion.Should().NotBeNull();
        inquilino.ModificadoPorId.Should().Be(7);
        inquilino.FechaModificacion.Should().NotBeNull();
    }

    [Fact]
    public void Modified_SoloActualizaModificado_SinTocarCreado()
    {
        ConfigurarUsuarioAutenticado(7);
        using var context = CrearContexto();
        var inquilino = new Inquilino { NombreCompleto = "Test", Identificacion = "1", UsuarioId = 7, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        context.SaveChanges();
        var fechaCreacionOriginal = inquilino.FechaCreacion;

        ConfigurarUsuarioAutenticado(9);
        inquilino.NombreCompleto = "Test Modificado";
        context.SaveChanges();

        inquilino.CreadoPorId.Should().Be(7);
        inquilino.FechaCreacion.Should().Be(fechaCreacionOriginal);
        inquilino.ModificadoPorId.Should().Be(9);
    }

    [Fact]
    public void Added_SinHttpContext_NoLanzaExcepcionYDejaCamposNulos()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        using var context = CrearContexto();

        var inquilino = new Inquilino { NombreCompleto = "Seed", Identificacion = "2", UsuarioId = 1, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        var accion = () => context.SaveChanges();

        accion.Should().NotThrow();
        inquilino.CreadoPorId.Should().BeNull();
        inquilino.ModificadoPorId.Should().BeNull();
        inquilino.FechaCreacion.Should().NotBeNull();
    }

    public void Dispose() => _connection.Dispose();
}
