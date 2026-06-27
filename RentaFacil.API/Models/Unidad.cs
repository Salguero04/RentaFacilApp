using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentaFacil.API.Models;

public class Unidad : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoRenta { get; set; }

    public bool Ocupada { get; set; }

    public int InmuebleId { get; set; }

    public Inmueble Inmueble { get; set; } = null!;

    public int UsuarioId { get; set; }

    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
