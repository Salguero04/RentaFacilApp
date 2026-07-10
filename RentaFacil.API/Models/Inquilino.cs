using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Inquilino : IAuditable
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

    // Cuenta de acceso del inquilino (auth.Usuarios). Null = aún no se ha registrado en la app.
    public int? UsuarioCuentaId { get; set; }

    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
