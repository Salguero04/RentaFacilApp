using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Unidad
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    public decimal MontoRenta { get; set; }

    public bool Ocupada { get; set; }

    public int InmuebleId { get; set; }

    public Inmueble Inmueble { get; set; } = null!;
}
