namespace RentaFacil.Shared.Models;

public record CrearInquilinoDto(
    string NombreCompleto,
    string Identificacion,
    string? Telefono,
    string? FotoUrl,
    int UsuarioId
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
