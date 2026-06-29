using System.ComponentModel.DataAnnotations.Schema;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

// Vínculo de un inquilino a un medidor (pantalla "Vincular Inquilino"), con su lectura
// y cómo se le cobra (tarifa por consumo, prorrateo de la planilla, o monto manual/fijo).
public class MedidorInquilino : IAuditable
{
    public int Id { get; set; }

    public int MedidorId { get; set; }

    public int InquilinoId { get; set; }

    public int? ContratoId { get; set; }

    public MetodoCobroInquilino MetodoCobro { get; set; }

    // Monto fijo cuando MetodoCobro == Manual.
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoFijo { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LecturaAnterior { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LecturaActual { get; set; }

    public bool Activo { get; set; } = true;

    public Medidor Medidor { get; set; } = null!;
    public Inquilino Inquilino { get; set; } = null!;

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
