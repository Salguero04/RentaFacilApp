using System.ComponentModel.DataAnnotations;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

public class Inmueble
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(255)]
    public string Direccion { get; set; } = null!;

    public TipoInmueble Tipo { get; set; }

    public decimal MontoRenta { get; set; }

    public int UsuarioId { get; set; }

    public ICollection<Unidad> Unidades { get; set; } = new List<Unidad>();
}
