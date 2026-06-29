using RentaFacil.Shared.Enums;

namespace RentaFacil.Shared.Models;

// ── Desglose de servicios pagados en un Pago (para recibo / Ingresos) ──
public record DetalleServicioPagoDto(
    TipoServicio Tipo,
    decimal Monto
);

// ── Medidor ─────────────────────────────────────────────────────────
public record MedidorDto(
    int Id,
    string Nombre,
    TipoServicio Tipo,
    int InmuebleId,
    string InmuebleNombre,
    ModoMedidor Modo,
    bool SubConsumoHabilitado,
    decimal Tarifa,
    bool Activo,
    int InquilinosCount
);

public record CrearMedidorDto(
    string Nombre,
    TipoServicio Tipo,
    int InmuebleId,
    ModoMedidor Modo,
    bool SubConsumoHabilitado,
    decimal Tarifa
);

// ── Vínculo medidor ↔ inquilino (con lectura y método de cobro) ──────
public record MedidorInquilinoDto(
    int Id,
    int MedidorId,
    int InquilinoId,
    string InquilinoNombre,
    int? ContratoId,
    MetodoCobroInquilino MetodoCobro,
    decimal MontoFijo,
    decimal LecturaAnterior,
    decimal LecturaActual,
    bool Activo
);

public record CrearMedidorInquilinoDto(
    int MedidorId,
    int InquilinoId,
    int? ContratoId,
    MetodoCobroInquilino MetodoCobro,
    decimal MontoFijo,
    decimal LecturaAnterior,
    decimal LecturaActual
);

// ── Factura / planilla real de un medidor ───────────────────────────
public record FacturaMedidorDto(
    int Id,
    int MedidorId,
    int Mes,
    int Anio,
    decimal MontoReal,
    DateTime FechaRegistro
);

public record CrearFacturaMedidorDto(
    int MedidorId,
    int Mes,
    int Anio,
    decimal MontoReal
);

// ── Reporte de Medidores (Ingresos): cobrado vs factura vs neto ─────
// Neto = CobradoAInquilinos − CostoReal (negativo = pérdida que asume el arrendador).
public record ResumenMedidorDto(
    int MedidorId,
    string MedidorNombre,
    string InmuebleNombre,
    TipoServicio Tipo,
    ModoMedidor Modo,
    decimal CobradoAInquilinos,
    decimal CostoReal,
    decimal Neto
);

// ── Notificación pendiente (hook para la futura app del inquilino) ───
public record NotificacionPendienteDto(
    int Id,
    int ContratoId,
    int InquilinoId,
    string Tipo,
    string? Detalle,
    DateTime Fecha,
    bool Notificado
);
