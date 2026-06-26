using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Pago : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }

    public decimal TotalMonto { get; set; }

    public decimal ACuenta { get; set; }

    public decimal Servicios { get; set; }

    public DateTime FechaPago { get; set; }

    [Required, MaxLength(20)]
    public string Periodo { get; set; } = null!;

    public bool Facturado { get; set; }

    public bool Completado { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
