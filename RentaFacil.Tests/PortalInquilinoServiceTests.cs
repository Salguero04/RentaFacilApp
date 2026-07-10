using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;

namespace RentaFacil.Tests;

public class PortalInquilinoServiceTests
{
    private readonly Mock<IPortalInquilinoRepository> _portalRepoMock;
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IReciboService> _reciboServiceMock;
    private readonly PortalInquilinoService _service;

    public PortalInquilinoServiceTests()
    {
        _portalRepoMock = new Mock<IPortalInquilinoRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _reciboServiceMock = new Mock<IReciboService>();
        _service = new PortalInquilinoService(_portalRepoMock.Object, _usuarioRepoMock.Object, _reciboServiceMock.Object);
    }

    [Fact]
    public async Task GetPagos_SoloDevuelvePagosDeContratosDeSusInquilinosVinculados()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var contratos = new List<Contrato> { new() { Id = 10, InquilinoId = 5, UsuarioId = 1 } };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.Is<List<int>>(l => l.SequenceEqual(new[] { 5 })))).ReturnsAsync(contratos);

        var pagos = new List<Pago>
        {
            new() { Id = 100, ContratoId = 10, Periodo = "MAY-26", TotalMonto = 500, ACuenta = 200, Servicios = 0, FechaPago = DateTime.Now, Completado = false, UsuarioId = 1 }
        };
        _portalRepoMock.Setup(r => r.GetPagosPorContratosAsync(It.Is<List<int>>(l => l.SequenceEqual(new[] { 10 })))).ReturnsAsync(pagos);

        var resultado = await _service.GetPagosAsync(77);

        resultado.Should().HaveCount(1);
        resultado.First().PagoId.Should().Be(100);
        resultado.First().ContratoId.Should().Be(10);
    }

    [Fact]
    public async Task GetPagos_CuentaSinVinculos_DevuelveVacio()
    {
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(new List<Inquilino>());

        var resultado = await _service.GetPagosAsync(77);

        resultado.Should().BeEmpty();
        _portalRepoMock.Verify(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>()), Times.Never);
        _portalRepoMock.Verify(r => r.GetPagosPorContratosAsync(It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task GetReciboPago_PagoDeOtroInquilino_DevuelveNull()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var contratos = new List<Contrato> { new() { Id = 10, InquilinoId = 5, UsuarioId = 1 } };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>())).ReturnsAsync(contratos);

        // El pago 999 pertenece a otro contrato (ajeno a esta cuenta) — no aparece en la lista devuelta.
        var pagos = new List<Pago>
        {
            new() { Id = 100, ContratoId = 10, Periodo = "MAY-26", TotalMonto = 500, ACuenta = 200, Servicios = 0, FechaPago = DateTime.Now, Completado = false, UsuarioId = 1 }
        };
        _portalRepoMock.Setup(r => r.GetPagosPorContratosAsync(It.IsAny<List<int>>())).ReturnsAsync(pagos);

        var resultado = await _service.GetReciboPagoAsync(999, 77, "carta");

        resultado.Should().BeNull();
        _reciboServiceMock.Verify(s => s.GenerarReciboPdfAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MarcarNotificacionLeida_DeOtroInquilino_DevuelveFalse()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var notificacionAjena = new NotificacionPendiente { Id = 50, ContratoId = 20, InquilinoId = 999, Tipo = "ContratoEditado", Fecha = DateTime.Now, Notificado = false, UsuarioId = 1 };
        _portalRepoMock.Setup(r => r.GetNotificacionAsync(50)).ReturnsAsync(notificacionAjena);

        var resultado = await _service.MarcarNotificacionLeidaAsync(50, 77);

        resultado.Should().BeFalse();
        _portalRepoMock.Verify(r => r.MarcarNotificadaAsync(It.IsAny<NotificacionPendiente>()), Times.Never);
    }

    [Fact]
    public async Task GetContratos_MapeaNombreArrendadorInmuebleYUnidad()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var inmueble = new Inmueble { Id = 1, Nombre = "Edificio Central", Direccion = "Av. Principal 123", UsuarioId = 1 };
        var unidad = new Unidad { Id = 1, Nombre = "Apt 1A", MontoRenta = 500, InmuebleId = 1, Inmueble = inmueble, UsuarioId = 1 };
        var contrato = new Contrato
        {
            Id = 10, InquilinoId = 5, UnidadId = 1, Unidad = unidad, Monto = 500, Garantia = 500,
            Frecuencia = FrecuenciaPago.Mensual, DiaPago = 5, FechaInicio = DateTime.Now, FechaFin = DateTime.Now.AddMonths(12),
            Activo = true, UsuarioId = 1
        };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Contrato> { contrato });

        var arrendador = new Usuario { Id = 1, NombreUsuario = "mario", Rol = "Propietario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(arrendador);

        var resultado = await _service.GetContratosAsync(77);

        resultado.Should().HaveCount(1);
        var dto = resultado.First();
        dto.NombreArrendador.Should().Be("mario");
        dto.NombreUnidad.Should().Be("Apt 1A");
        dto.NombreInmueble.Should().Be("Edificio Central");
    }
}
