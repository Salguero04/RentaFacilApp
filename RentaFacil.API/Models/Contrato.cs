using System.ComponentModel.DataAnnotations;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

public class Contrato
{
    public int Id { get; set; }

    public int InquilinoId { get; set; }

    public int UnidadId { get; set; }

    public decimal Monto { get; set; }

    public decimal Garantia { get; set; }

    public FrecuenciaPago Frecuencia { get; set; }

    public int DuracionMeses { get; set; }

    public int DiaPago { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public Inquilino Inquilino { get; set; } = null!;
    
    public Unidad Unidad { get; set; } = null!;
}
