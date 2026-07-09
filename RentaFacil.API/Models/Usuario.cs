using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = null!;

    [MaxLength(150)]
    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    [MaxLength(255)]
    public string? GoogleId { get; set; }

    [Required, MaxLength(30)]
    public string Rol { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
}
