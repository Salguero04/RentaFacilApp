using RentaFacil.Shared.Enums;

namespace RentaFacil.Shared.Models;

public record CrearContratoDto(
    int InquilinoId,
    int UnidadId,
    decimal Monto,
    decimal Garantia,
    FrecuenciaPago Frecuencia,
    int DuracionMeses,
    int DiaPago,
    DateTime FechaInicio,
    string? Observaciones
);

public record ContratoDto(
    int Id,
    int InquilinoId,
    int UnidadId,
    decimal Monto,
    decimal Garantia,
    FrecuenciaPago Frecuencia,
    int DuracionMeses,
    int DiaPago,
    DateTime FechaInicio,
    DateTime FechaFin,
    string? Observaciones,
    bool Activo
);
