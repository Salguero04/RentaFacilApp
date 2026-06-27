namespace RentaFacil.Shared.Models;

public record CrearRecordatorioDto(
    int InquilinoId,
    int? ContratoId,
    string Detalle,
    DateTime FechaProgramada
);

public record RecordatorioDto(
    int Id,
    int InquilinoId,
    int? ContratoId,
    string Detalle,
    DateTime FechaProgramada
);
