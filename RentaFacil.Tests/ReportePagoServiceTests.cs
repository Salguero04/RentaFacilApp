using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class ReportePagoServiceTests
{
    private readonly Mock<IReportePagoRepository> _reportePagoRepoMock;
    private readonly Mock<IPortalInquilinoRepository> _portalRepoMock;
    private readonly Mock<IInquilinoRepository> _inquilinoRepoMock;
    private readonly Mock<IDataChangeNotifier> _notifierMock;
    private readonly ReportePagoService _service;

    public ReportePagoServiceTests()
    {
        _reportePagoRepoMock = new Mock<IReportePagoRepository>();
        _portalRepoMock = new Mock<IPortalInquilinoRepository>();
        _inquilinoRepoMock = new Mock<IInquilinoRepository>();
        _notifierMock = new Mock<IDataChangeNotifier>();
        _service = new ReportePagoService(_reportePagoRepoMock.Object, _portalRepoMock.Object, _inquilinoRepoMock.Object, _notifierMock.Object);
    }

    [Fact]
    public async Task CrearReporte_ContratoNoVinculadoASuCuenta_DevuelveNull()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        // El contrato 10 es de este inquilino, pero el reporte pide el contrato 999 (ajeno).
        var contratos = new List<Contrato> { new() { Id = 10, InquilinoId = 5, UsuarioId = 1 } };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>())).ReturnsAsync(contratos);

        var dto = new CrearReportePagoDto(999, 500, null, null);

        var resultado = await _service.CrearAsync(dto, 77);

        resultado.Should().BeNull();
        _reportePagoRepoMock.Verify(r => r.AddAsync(It.IsAny<ReportePago>()), Times.Never);
        _notifierMock.Verify(n => n.NotificarCambioAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CrearReporte_FotoMayorA1MB_DevuelveNull()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var contratos = new List<Contrato> { new() { Id = 10, InquilinoId = 5, UsuarioId = 1 } };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>())).ReturnsAsync(contratos);

        var fotoDemasiadoGrande = new byte[1_048_577];
        var dto = new CrearReportePagoDto(10, 500, null, fotoDemasiadoGrande);

        var resultado = await _service.CrearAsync(dto, 77);

        resultado.Should().BeNull();
        _reportePagoRepoMock.Verify(r => r.AddAsync(It.IsAny<ReportePago>()), Times.Never);
    }

    [Fact]
    public async Task CrearReporte_Valido_PersisteConEstadoPendienteYNotificaPorSignalR()
    {
        var inquilinos = new List<Inquilino> { new() { Id = 5, NombreCompleto = "Juan", Identificacion = "123", UsuarioId = 1, UsuarioCuentaId = 77 } };
        _portalRepoMock.Setup(r => r.GetInquilinosPorCuentaAsync(77)).ReturnsAsync(inquilinos);

        var contratos = new List<Contrato> { new() { Id = 10, InquilinoId = 5, UsuarioId = 1 } };
        _portalRepoMock.Setup(r => r.GetContratosPorInquilinosAsync(It.IsAny<List<int>>())).ReturnsAsync(contratos);

        _reportePagoRepoMock.Setup(r => r.AddAsync(It.IsAny<ReportePago>()))
            .ReturnsAsync((ReportePago r) => { r.Id = 1; return r; });

        var dto = new CrearReportePagoDto(10, 500, "Transferencia realizada", null);

        var resultado = await _service.CrearAsync(dto, 77);

        resultado.Should().NotBeNull();
        resultado!.Estado.Should().Be(EstadoReportePago.Pendiente);
        resultado.NombreInquilino.Should().Be("Juan");
        resultado.InquilinoId.Should().Be(5);
        resultado.ContratoId.Should().Be(10);

        _reportePagoRepoMock.Verify(r => r.AddAsync(It.Is<ReportePago>(rp =>
            rp.Estado == EstadoReportePago.Pendiente && rp.UsuarioId == 1 && rp.CuentaInquilinoId == 77 && rp.InquilinoId == 5)), Times.Once);
        _notifierMock.Verify(n => n.NotificarCambioAsync("ReportePago", 1, "crear"), Times.Once);
    }

    [Fact]
    public async Task Confirmar_ReporteDeOtroArrendador_DevuelveFalse()
    {
        // GetByIdAsync ya filtra por usuarioId a nivel repo: si el reporte es de otro arrendador, devuelve null.
        _reportePagoRepoMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync((ReportePago?)null);

        var resultado = await _service.ConfirmarAsync(1, 99);

        resultado.Should().BeFalse();
        _reportePagoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ReportePago>()), Times.Never);
    }

    [Fact]
    public async Task Confirmar_ReportePendiente_CambiaEstadoADevuelveTrue()
    {
        var reporte = new ReportePago { Id = 1, ContratoId = 10, InquilinoId = 5, Monto = 500, Estado = EstadoReportePago.Pendiente, UsuarioId = 1, CuentaInquilinoId = 77 };
        _reportePagoRepoMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(reporte);

        var resultado = await _service.ConfirmarAsync(1, 1);

        resultado.Should().BeTrue();
        reporte.Estado.Should().Be(EstadoReportePago.Confirmado);
        _reportePagoRepoMock.Verify(r => r.UpdateAsync(It.Is<ReportePago>(rp => rp.Estado == EstadoReportePago.Confirmado)), Times.Once);
    }

    [Fact]
    public async Task Rechazar_ReportePendiente_CambiaEstado()
    {
        var reporte = new ReportePago { Id = 1, ContratoId = 10, InquilinoId = 5, Monto = 500, Estado = EstadoReportePago.Pendiente, UsuarioId = 1, CuentaInquilinoId = 77 };
        _reportePagoRepoMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(reporte);

        var resultado = await _service.RechazarAsync(1, 1);

        resultado.Should().BeTrue();
        reporte.Estado.Should().Be(EstadoReportePago.Rechazado);
        _reportePagoRepoMock.Verify(r => r.UpdateAsync(It.Is<ReportePago>(rp => rp.Estado == EstadoReportePago.Rechazado)), Times.Once);
    }

    [Fact]
    public async Task Confirmar_ReporteYaConfirmado_DevuelveFalse()
    {
        var reporte = new ReportePago { Id = 1, ContratoId = 10, InquilinoId = 5, Monto = 500, Estado = EstadoReportePago.Confirmado, UsuarioId = 1, CuentaInquilinoId = 77 };
        _reportePagoRepoMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(reporte);

        var resultado = await _service.ConfirmarAsync(1, 1);

        resultado.Should().BeFalse();
        _reportePagoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ReportePago>()), Times.Never);
    }
}
