namespace RentaFacil.Shared.Enums;

// Tipos de servicio básico que un medidor puede gestionar.
// El int se mantiene estable: Agua=0, Electricidad=1 (antes "Luz"), Gas=2, Otro=3.
public enum TipoServicio
{
    Agua = 0,
    Electricidad = 1,
    Gas = 2,
    Otro = 3
}
