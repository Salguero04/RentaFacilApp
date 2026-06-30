using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Enums;

namespace RentaFacil.Tests;

public class MedidorServiceTests
{
    private readonly Mock<IMedidorRepository> _medidorRepo = new();
    private readonly Mock<IMedidorInquilinoRepository> _vinculoRepo = new();
    private readonly Mock<IFacturaMedidorRepository> _facturaRepo = new();
    private readonly Mock<IInmuebleRepository> _inmuebleRepo = new();
    private readonly Mock<IInquilinoRepository> _inquilinoRepo = new();
    private readonly MedidorService _service;

    public MedidorServiceTests()
    {
        _service = new MedidorService(_medidorRepo.Object, _vinculoRepo.Object, _facturaRepo.Object, _inmuebleRepo.Object, _inquilinoRepo.Object);
    }

    private Medidor Medidor(ModoMedidor modo, decimal tarifa, params MedidorInquilino[] vinculos)
    {
        var m = new Medidor
        {
            Id = 1, Nombre = "Med", Tipo = TipoServicio.Electricidad, Modo = modo, Tarifa = tarifa,
            Activo = true, UsuarioId = 1, Inmueble = new Inmueble { Nombre = "Edif" },
            Inquilinos = vinculos.ToList()
        };
        return m;
    }

    [Fact]
    public async Task Resumen_PorLecturaTarifa_CalculaConsumoPorTarifa()
    {
        var v = new MedidorInquilino { Id = 10, Activo = true, MetodoCobro = MetodoCobroInquilino.Tarifa, LecturaAnterior = 100, LecturaActual = 130 };
        _medidorRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Medidor> { Medidor(ModoMedidor.PorLectura, 0.5m, v) });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>());

        var res = (await _service.CalcularResumenAsync(1, 6, 2026)).Single();

        res.CobradoAInquilinos.Should().Be(15m); // (130-100) * 0.5
        res.CostoReal.Should().Be(0);
        res.Neto.Should().Be(15m);
    }

    [Fact]
    public async Task Resumen_Manual_UsaMontoFijo()
    {
        var v = new MedidorInquilino { Id = 10, Activo = true, MetodoCobro = MetodoCobroInquilino.Manual, MontoFijo = 15 };
        _medidorRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Medidor> { Medidor(ModoMedidor.PorPlanilla, 0, v) });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>());

        var res = (await _service.CalcularResumenAsync(1, 6, 2026)).Single();

        res.CobradoAInquilinos.Should().Be(15);
    }

    [Fact]
    public async Task Resumen_ProrrateoProporcional_RepartePorConsumo()
    {
        var v1 = new MedidorInquilino { Id = 10, Activo = true, MetodoCobro = MetodoCobroInquilino.Prorrateo, LecturaAnterior = 0, LecturaActual = 30 };
        var v2 = new MedidorInquilino { Id = 11, Activo = true, MetodoCobro = MetodoCobroInquilino.Prorrateo, LecturaAnterior = 0, LecturaActual = 20 };
        _medidorRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Medidor> { Medidor(ModoMedidor.PorPlanilla, 0, v1, v2) });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>
        {
            new() { MedidorId = 1, Mes = 6, Anio = 2026, MontoReal = 50 }
        });

        var res = (await _service.CalcularResumenAsync(1, 6, 2026)).Single();

        res.CobradoAInquilinos.Should().Be(50m); // 30 + 20
        res.CostoReal.Should().Be(50m);
        res.Neto.Should().Be(0m);
    }

    [Fact]
    public async Task Resumen_ProrrateoSinLecturas_RepartePartesIguales()
    {
        var v1 = new MedidorInquilino { Id = 10, Activo = true, MetodoCobro = MetodoCobroInquilino.Prorrateo };
        var v2 = new MedidorInquilino { Id = 11, Activo = true, MetodoCobro = MetodoCobroInquilino.Prorrateo };
        _medidorRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Medidor> { Medidor(ModoMedidor.PorPlanilla, 0, v1, v2) });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>
        {
            new() { MedidorId = 1, Mes = 6, Anio = 2026, MontoReal = 50 }
        });

        var res = (await _service.CalcularResumenAsync(1, 6, 2026)).Single();

        res.CobradoAInquilinos.Should().Be(50m); // 25 + 25
    }

    [Fact]
    public async Task Resumen_MontoFijoMenorQuePlanilla_ReportaPerdida()
    {
        var v = new MedidorInquilino { Id = 10, Activo = true, MetodoCobro = MetodoCobroInquilino.Manual, MontoFijo = 15 };
        _medidorRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Medidor> { Medidor(ModoMedidor.PorPlanilla, 0, v) });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>
        {
            new() { MedidorId = 1, Mes = 6, Anio = 2026, MontoReal = 50 }
        });

        var res = (await _service.CalcularResumenAsync(1, 6, 2026)).Single();

        res.CobradoAInquilinos.Should().Be(15m);
        res.CostoReal.Should().Be(50m);
        res.Neto.Should().Be(-35m); // pérdida asumida por el arrendador
    }

    [Fact]
    public async Task ActualizarVinculo_VinculoExistenteYEntidadesPropias_RetornaTrue()
    {
        var vinculo = new MedidorInquilino { Id = 5, MedidorId = 1, InquilinoId = 2, UsuarioId = 1, MetodoCobro = MetodoCobroInquilino.Manual, MontoFijo = 10 };
        var medidor = new Medidor { Id = 1, UsuarioId = 1, Nombre = "Med", Inquilinos = new List<MedidorInquilino>(), Inmueble = new Inmueble { Nombre = "Edif" } };
        var inquilino = new Inquilino { Id = 2, UsuarioId = 1, NombreCompleto = "Inquilino Dos" };
        _vinculoRepo.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(vinculo);
        _medidorRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(medidor);
        _inquilinoRepo.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(inquilino);
        _vinculoRepo.Setup(r => r.UpdateAsync(It.IsAny<MedidorInquilino>())).Returns(Task.CompletedTask);

        var dto = new RentaFacil.Shared.Models.CrearMedidorInquilinoDto(1, 2, null, MetodoCobroInquilino.Tarifa, 0, 100, 150);
        var resultado = await _service.ActualizarVinculoAsync(5, dto, 1);

        resultado.Should().BeTrue();
        _vinculoRepo.Verify(r => r.UpdateAsync(It.Is<MedidorInquilino>(v => v.LecturaActual == 150 && v.MetodoCobro == MetodoCobroInquilino.Tarifa)), Times.Once);
    }

    [Fact]
    public async Task ActualizarVinculo_VinculoNoExiste_RetornaFalse()
    {
        _vinculoRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((MedidorInquilino?)null);

        var dto = new RentaFacil.Shared.Models.CrearMedidorInquilinoDto(1, 2, null, MetodoCobroInquilino.Manual, 20, 0, 0);
        var resultado = await _service.ActualizarVinculoAsync(99, dto, 1);

        resultado.Should().BeFalse();
        _vinculoRepo.Verify(r => r.UpdateAsync(It.IsAny<MedidorInquilino>()), Times.Never);
    }

    [Fact]
    public async Task CalcularCobrosContrato_DevuelveDetallePorTipo()
    {
        var medidor = Medidor(ModoMedidor.PorLectura, 0.5m);
        var v = new MedidorInquilino { Id = 10, MedidorId = 1, ContratoId = 100, Activo = true, MetodoCobro = MetodoCobroInquilino.Tarifa, LecturaAnterior = 100, LecturaActual = 130, Medidor = medidor };
        _vinculoRepo.Setup(r => r.GetByContratoAsync(100, 1)).ReturnsAsync(new List<MedidorInquilino> { v });
        _vinculoRepo.Setup(r => r.GetByMedidorAsync(1, 1)).ReturnsAsync(new List<MedidorInquilino> { v });
        _facturaRepo.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<FacturaMedidor>());

        var detalles = (await _service.CalcularCobrosContratoAsync(1, 100, 6, 2026)).ToList();

        detalles.Should().ContainSingle();
        detalles[0].Tipo.Should().Be(TipoServicio.Electricidad);
        detalles[0].Monto.Should().Be(15m);
    }
}
