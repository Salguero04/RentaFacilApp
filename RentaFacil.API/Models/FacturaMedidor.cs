using System.ComponentModel.DataAnnotations.Schema;

namespace RentaFacil.API.Models;

// La factura/planilla real de un medidor en un periodo (lo que paga el arrendador a la empresa).
// Se compara contra lo cobrado a inquilinos para reportar pérdida/ganancia.
public class FacturaMedidor : IAuditable
{
    public int Id { get; set; }

    public int MedidorId { get; set; }

    public int Mes { get; set; }   // 1-12
    public int Anio { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoReal { get; set; }

    public DateTime FechaRegistro { get; set; }

    public Medidor Medidor { get; set; } = null!;

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
