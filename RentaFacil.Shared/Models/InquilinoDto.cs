using RentaFacil.Shared.Validaciones;

namespace RentaFacil.Shared.Models;

public record CrearInquilinoDto(
    string NombreCompleto,
    [IdentificacionEcuatoriana] string Identificacion,
    string? Telefono,
    string? FotoUrl
);

public record InquilinoDto(
    int Id,
    string NombreCompleto,
    string Identificacion,
    string? Telefono,
    string? FotoUrl,
    DateTime FechaRegistro,
    int UsuarioId
);
