using RentaFacil.Shared.Enums;

namespace RentaFacil.Shared.Models;

public record CrearInmuebleDto(
    string Nombre,
    string Direccion,
    TipoInmueble Tipo,
    decimal MontoRenta
);

public record InmuebleDto(
    int Id,
    string Nombre,
    string Direccion,
    TipoInmueble Tipo,
    decimal MontoRenta,
    int UsuarioId
);

public record CrearUnidadDto(
    string Nombre,
    decimal MontoRenta,
    int InmuebleId
);

public record UnidadDto(
    int Id,
    string Nombre,
    decimal MontoRenta,
    bool Ocupada,
    int InmuebleId
);
