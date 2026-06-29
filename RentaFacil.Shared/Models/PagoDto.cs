namespace RentaFacil.Shared.Models;

public record CrearPagoDto(
    int ContratoId,
    decimal TotalMonto,
    decimal ACuenta,
    decimal Servicios,
    DateTime FechaPago,
    string Periodo,
    bool Facturado,
    List<DetalleServicioPagoDto>? Detalles = null
);

public record PagoDto(
    int Id,
    int ContratoId,
    decimal TotalMonto,
    decimal ACuenta,
    decimal Servicios,
    DateTime FechaPago,
    string Periodo,
    bool Facturado,
    bool Completado,
    List<DetalleServicioPagoDto>? Detalles = null
);
