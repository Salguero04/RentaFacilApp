using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

// "Ya pagué": el inquilino lo reporta desde su portal; el arrendador lo confirma o rechaza.
// Confirmar NO crea el Pago automáticamente: el arrendador lo registra en CrearPago como siempre.
public class ReportePago
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public int InquilinoId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [MaxLength(500)]
    public string? Comentario { get; set; }

    public byte[]? FotoComprobante { get; set; }    // JPEG/PNG, máx 1 MB (valida el service)

    public DateTime FechaReporte { get; set; }
    public EstadoReportePago Estado { get; set; }

    public int UsuarioId { get; set; }              // arrendador dueño (para su bandeja)
    public int CuentaInquilinoId { get; set; }      // auth.Usuarios que lo reportó
}
