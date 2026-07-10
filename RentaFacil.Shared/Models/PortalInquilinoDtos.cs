namespace RentaFacil.Shared.Models;

// Código de vinculación generado por el arrendador para un contrato: se muestra como QR
// para que el inquilino cree su cuenta (o vincule un contrato adicional) escaneándolo.
public record CodigoVinculacionDto(string Codigo, DateTime FechaExpiracion);
