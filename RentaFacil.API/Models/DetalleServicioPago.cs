using System.ComponentModel.DataAnnotations.Schema;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

// Desglose de cuánto pagó el inquilino por cada servicio en un Pago concreto.
// Permite reportar agua vs luz por separado en Ingresos y en el recibo.
// Siempre se accede vía su Pago padre (ya filtrado por UsuarioId).
public class DetalleServicioPago
{
    public int Id { get; set; }

    public int PagoId { get; set; }

    public TipoServicio Tipo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    public Pago Pago { get; set; } = null!;

    public int UsuarioId { get; set; }
}
