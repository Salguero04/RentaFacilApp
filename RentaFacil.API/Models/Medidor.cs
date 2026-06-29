using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

// Un medidor de servicio (agua/electricidad/gas) que el arrendador gestiona en un inmueble.
// Puede facturar por lectura (sub-medidor propio) o por planilla (factura de la empresa).
public class Medidor : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    public TipoServicio Tipo { get; set; }

    public int InmuebleId { get; set; }

    public ModoMedidor Modo { get; set; }

    // Habilita prorratear la factura entre varios inquilinos vinculados.
    public bool SubConsumoHabilitado { get; set; }

    // Precio por unidad (m³/kWh) cuando el cobro es por lectura+tarifa.
    [Column(TypeName = "decimal(18,4)")]
    public decimal Tarifa { get; set; }

    public bool Activo { get; set; } = true;

    public Inmueble Inmueble { get; set; } = null!;

    public ICollection<MedidorInquilino> Inquilinos { get; set; } = new List<MedidorInquilino>();
    public ICollection<FacturaMedidor> Facturas { get; set; } = new List<FacturaMedidor>();

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
