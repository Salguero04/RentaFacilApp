using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class MedidorService : IMedidorService
{
    private readonly IMedidorRepository _medidorRepo;
    private readonly IMedidorInquilinoRepository _vinculoRepo;
    private readonly IFacturaMedidorRepository _facturaRepo;
    private readonly IInmuebleRepository _inmuebleRepo;
    private readonly IInquilinoRepository _inquilinoRepo;

    public MedidorService(
        IMedidorRepository medidorRepo,
        IMedidorInquilinoRepository vinculoRepo,
        IFacturaMedidorRepository facturaRepo,
        IInmuebleRepository inmuebleRepo,
        IInquilinoRepository inquilinoRepo)
    {
        _medidorRepo = medidorRepo;
        _vinculoRepo = vinculoRepo;
        _facturaRepo = facturaRepo;
        _inmuebleRepo = inmuebleRepo;
        _inquilinoRepo = inquilinoRepo;
    }

    // ── Medidores ────────────────────────────────────────────────────
    public async Task<IEnumerable<MedidorDto>> GetMedidoresAsync(int usuarioId)
    {
        var medidores = await _medidorRepo.GetAllAsync(usuarioId);
        return medidores.Select(MapMedidor);
    }

    public async Task<MedidorDto?> CrearMedidorAsync(CrearMedidorDto dto, int usuarioId)
    {
        var inmueble = await _inmuebleRepo.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return null;

        var medidor = new Medidor
        {
            Nombre = dto.Nombre, Tipo = dto.Tipo, InmuebleId = dto.InmuebleId,
            Modo = dto.Modo, SubConsumoHabilitado = dto.SubConsumoHabilitado,
            Tarifa = dto.Tarifa, Activo = true, UsuarioId = usuarioId
        };
        var creado = await _medidorRepo.AddAsync(medidor);
        creado.Inmueble = inmueble;
        return MapMedidor(creado);
    }

    public async Task<bool> UpdateMedidorAsync(int id, CrearMedidorDto dto, int usuarioId)
    {
        var medidor = await _medidorRepo.GetByIdAsync(id, usuarioId);
        if (medidor == null) return false;
        var inmueble = await _inmuebleRepo.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return false;

        medidor.Nombre = dto.Nombre; medidor.Tipo = dto.Tipo; medidor.InmuebleId = dto.InmuebleId;
        medidor.Modo = dto.Modo; medidor.SubConsumoHabilitado = dto.SubConsumoHabilitado; medidor.Tarifa = dto.Tarifa;
        await _medidorRepo.UpdateAsync(medidor);
        return true;
    }

    public async Task DeleteMedidorAsync(int id, int usuarioId) => await _medidorRepo.DeleteAsync(id, usuarioId);

    // ── Vínculos ─────────────────────────────────────────────────────
    public async Task<IEnumerable<MedidorInquilinoDto>> GetVinculosAsync(int medidorId, int usuarioId)
    {
        var vinculos = await _vinculoRepo.GetByMedidorAsync(medidorId, usuarioId);
        return vinculos.Select(MapVinculo);
    }

    public async Task<MedidorInquilinoDto?> VincularInquilinoAsync(CrearMedidorInquilinoDto dto, int usuarioId)
    {
        var medidor = await _medidorRepo.GetByIdAsync(dto.MedidorId, usuarioId);
        var inquilino = await _inquilinoRepo.GetByIdAsync(dto.InquilinoId, usuarioId);
        if (medidor == null || inquilino == null) return null;

        var vinculo = new MedidorInquilino
        {
            MedidorId = dto.MedidorId, InquilinoId = dto.InquilinoId, ContratoId = dto.ContratoId,
            MetodoCobro = dto.MetodoCobro, MontoFijo = dto.MontoFijo,
            LecturaAnterior = dto.LecturaAnterior, LecturaActual = dto.LecturaActual,
            Activo = true, UsuarioId = usuarioId
        };
        var creado = await _vinculoRepo.AddAsync(vinculo);
        creado.Inquilino = inquilino;
        return MapVinculo(creado);
    }

    public async Task<bool> ActualizarVinculoAsync(int id, CrearMedidorInquilinoDto dto, int usuarioId)
    {
        var vinculo = await _vinculoRepo.GetByIdAsync(id, usuarioId);
        if (vinculo == null) return false;
        var medidor = await _medidorRepo.GetByIdAsync(dto.MedidorId, usuarioId);
        if (medidor == null) return false;
        var inquilino = await _inquilinoRepo.GetByIdAsync(dto.InquilinoId, usuarioId);
        if (inquilino == null) return false;

        vinculo.MedidorId = dto.MedidorId;
        vinculo.InquilinoId = dto.InquilinoId;
        vinculo.ContratoId = dto.ContratoId;
        vinculo.MetodoCobro = dto.MetodoCobro;
        vinculo.MontoFijo = dto.MontoFijo;
        vinculo.LecturaAnterior = dto.LecturaAnterior;
        vinculo.LecturaActual = dto.LecturaActual;
        await _vinculoRepo.UpdateAsync(vinculo);
        return true;
    }

    public async Task DesvincularAsync(int vinculoId, int usuarioId) => await _vinculoRepo.DeleteAsync(vinculoId, usuarioId);

    // ── Facturas ─────────────────────────────────────────────────────
    public async Task<IEnumerable<FacturaMedidorDto>> GetFacturasAsync(int medidorId, int usuarioId)
    {
        var facturas = await _facturaRepo.GetByMedidorAsync(medidorId, usuarioId);
        return facturas.Select(MapFactura);
    }

    public async Task<FacturaMedidorDto?> GuardarFacturaAsync(CrearFacturaMedidorDto dto, int usuarioId)
    {
        var medidor = await _medidorRepo.GetByIdAsync(dto.MedidorId, usuarioId);
        if (medidor == null) return null;

        var existente = await _facturaRepo.GetByClaveAsync(dto.MedidorId, dto.Mes, dto.Anio, usuarioId);
        if (existente != null)
        {
            existente.MontoReal = dto.MontoReal;
            existente.FechaRegistro = DateTime.Now;
            await _facturaRepo.UpdateAsync(existente);
            return MapFactura(existente);
        }

        var factura = new FacturaMedidor
        {
            MedidorId = dto.MedidorId, Mes = dto.Mes, Anio = dto.Anio,
            MontoReal = dto.MontoReal, FechaRegistro = DateTime.Now, UsuarioId = usuarioId
        };
        var creada = await _facturaRepo.AddAsync(factura);
        return MapFactura(creada);
    }

    public async Task DeleteFacturaAsync(int id, int usuarioId) => await _facturaRepo.DeleteAsync(id, usuarioId);

    // ── Reporte de Ingresos (por medidor) ────────────────────────────
    public async Task<IEnumerable<ResumenMedidorDto>> CalcularResumenAsync(int usuarioId, int mes, int anio)
    {
        var medidores = (await _medidorRepo.GetAllAsync(usuarioId)).Where(m => m.Activo).ToList();
        var facturas = (await _facturaRepo.GetAllAsync(usuarioId)).Where(f => f.Mes == mes && f.Anio == anio).ToList();

        var resultado = new List<ResumenMedidorDto>();
        foreach (var medidor in medidores)
        {
            var vinculos = medidor.Inquilinos.Where(v => v.Activo).ToList();
            var facturaMonto = facturas.Where(f => f.MedidorId == medidor.Id).Sum(f => f.MontoReal);
            var cobros = CalcularCobros(medidor, vinculos, facturaMonto);
            var cobrado = cobros.Values.Sum();
            var costoReal = facturaMonto;
            resultado.Add(new ResumenMedidorDto(
                medidor.Id, medidor.Nombre, medidor.Inmueble?.Nombre ?? "",
                medidor.Tipo, medidor.Modo, cobrado, costoReal, cobrado - costoReal));
        }
        return resultado;
    }

    // ── Cobro de servicios de un contrato (CrearPago) ────────────────
    public async Task<IEnumerable<DetalleServicioPagoDto>> CalcularCobrosContratoAsync(int usuarioId, int contratoId, int mes, int anio)
    {
        var vinculosContrato = (await _vinculoRepo.GetByContratoAsync(contratoId, usuarioId)).Where(v => v.Activo).ToList();
        if (vinculosContrato.Count == 0) return new List<DetalleServicioPagoDto>();

        var facturas = (await _facturaRepo.GetAllAsync(usuarioId)).Where(f => f.Mes == mes && f.Anio == anio).ToList();
        var detalles = new List<DetalleServicioPagoDto>();

        foreach (var grupo in vinculosContrato.GroupBy(v => v.MedidorId))
        {
            var medidor = grupo.First().Medidor;
            if (medidor == null) continue;
            // Para prorrateo se necesitan TODOS los vínculos del medidor (no solo los de este contrato).
            var todosVinculos = (await _vinculoRepo.GetByMedidorAsync(medidor.Id, usuarioId)).Where(v => v.Activo).ToList();
            var facturaMonto = facturas.Where(f => f.MedidorId == medidor.Id).Sum(f => f.MontoReal);
            var cobros = CalcularCobros(medidor, todosVinculos, facturaMonto);

            foreach (var v in grupo)
            {
                var monto = cobros.GetValueOrDefault(v.Id);
                if (monto != 0) detalles.Add(new DetalleServicioPagoDto(medidor.Tipo, monto));
            }
        }

        // Consolidar por tipo de servicio.
        return detalles.GroupBy(d => d.Tipo)
            .Select(g => new DetalleServicioPagoDto(g.Key, g.Sum(x => x.Monto)))
            .ToList();
    }

    // ── Cálculo central: monto por vínculo según método de cobro ──────
    private static Dictionary<int, decimal> CalcularCobros(Medidor medidor, List<MedidorInquilino> vinculos, decimal facturaMonto)
    {
        var result = new Dictionary<int, decimal>();
        var prorrateoVinculos = vinculos.Where(v => v.MetodoCobro == MetodoCobroInquilino.Prorrateo).ToList();
        var totalConsumoProrrateo = prorrateoVinculos.Sum(Consumo);

        foreach (var v in vinculos)
        {
            decimal monto = v.MetodoCobro switch
            {
                MetodoCobroInquilino.Tarifa => Consumo(v) * medidor.Tarifa,
                MetodoCobroInquilino.Manual => v.MontoFijo,
                MetodoCobroInquilino.Prorrateo => RepartirProrrateo(v, prorrateoVinculos, totalConsumoProrrateo, facturaMonto),
                _ => 0
            };
            result[v.Id] = decimal.Round(monto, 2);
        }
        return result;
    }

    private static decimal Consumo(MedidorInquilino v) => Math.Max(0, v.LecturaActual - v.LecturaAnterior);

    private static decimal RepartirProrrateo(MedidorInquilino v, List<MedidorInquilino> grupo, decimal totalConsumo, decimal facturaMonto)
    {
        if (grupo.Count == 0) return 0;
        if (totalConsumo > 0) return facturaMonto * (Consumo(v) / totalConsumo); // proporcional al consumo
        return facturaMonto / grupo.Count; // partes iguales si no hay lecturas
    }

    // ── Mapeos ───────────────────────────────────────────────────────
    private static MedidorDto MapMedidor(Medidor m) => new(
        m.Id, m.Nombre, m.Tipo, m.InmuebleId, m.Inmueble?.Nombre ?? "", m.Modo,
        m.SubConsumoHabilitado, m.Tarifa, m.Activo, m.Inquilinos?.Count(i => i.Activo) ?? 0);

    private static MedidorInquilinoDto MapVinculo(MedidorInquilino v) => new(
        v.Id, v.MedidorId, v.InquilinoId, v.Inquilino?.NombreCompleto ?? "", v.ContratoId,
        v.MetodoCobro, v.MontoFijo, v.LecturaAnterior, v.LecturaActual, v.Activo);

    private static FacturaMedidorDto MapFactura(FacturaMedidor f) => new(
        f.Id, f.MedidorId, f.Mes, f.Anio, f.MontoReal, f.FechaRegistro);
}
