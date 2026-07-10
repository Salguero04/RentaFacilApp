using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

// Código de un solo uso que el arrendador genera por contrato (se muestra como QR).
// El inquilino lo usa para crear su cuenta y quedar vinculado a ese contrato.
public class CodigoVinculacion
{
    public int Id { get; set; }

    [Required, MaxLength(8)]
    public string Codigo { get; set; } = null!;

    public int ContratoId { get; set; }
    public int InquilinoId { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }   // FechaCreacion + 7 días
    public DateTime? UsadoEn { get; set; }          // null = vigente si no expiró

    public int UsuarioId { get; set; }              // arrendador dueño
}
