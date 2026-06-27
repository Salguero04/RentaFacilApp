using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Recordatorio : IAuditable
{
    public int Id { get; set; }

    public int InquilinoId { get; set; }

    public int? ContratoId { get; set; }

    [Required, MaxLength(500)]
    public string Detalle { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }

    public Inquilino Inquilino { get; set; } = null!;

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
