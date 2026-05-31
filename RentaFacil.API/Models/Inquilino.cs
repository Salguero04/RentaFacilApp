using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Inquilino
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Identificacion { get; set; } = null!;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(255)]
    public string? FotoUrl { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int UsuarioId { get; set; }

    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
