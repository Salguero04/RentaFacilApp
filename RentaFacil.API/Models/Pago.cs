using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentaFacil.API.Models;

public class Pago : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMonto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ACuenta { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Servicios { get; set; }

    public DateTime FechaPago { get; set; }

    [Required, MaxLength(20)]
    public string Periodo { get; set; } = null!;

    public bool Facturado { get; set; }

    public bool Completado { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public ICollection<DetalleServicioPago> DetalleServicios { get; set; } = new List<DetalleServicioPago>();

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
