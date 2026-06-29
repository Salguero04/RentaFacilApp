using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

// Registro de un cambio que el inquilino debería ver cuando exista su app (ej. edición de contrato).
// Hoy solo se persiste; no dispara push. Hook para la futura app del inquilino.
public class NotificacionPendiente
{
    public int Id { get; set; }

    public int ContratoId { get; set; }

    public int InquilinoId { get; set; }

    [Required, MaxLength(50)]
    public string Tipo { get; set; } = null!;   // ej. "ContratoEditado"

    [MaxLength(500)]
    public string? Detalle { get; set; }

    public DateTime Fecha { get; set; }

    public bool Notificado { get; set; }

    public int UsuarioId { get; set; }
}
