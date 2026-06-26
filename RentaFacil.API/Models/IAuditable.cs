namespace RentaFacil.API.Models;

public interface IAuditable
{
    int? CreadoPorId { get; set; }
    DateTime? FechaCreacion { get; set; }
    int? ModificadoPorId { get; set; }
    DateTime? FechaModificacion { get; set; }
}
