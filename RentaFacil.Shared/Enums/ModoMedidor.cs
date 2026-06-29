namespace RentaFacil.Shared.Enums;

// Cómo se factura un medidor.
public enum ModoMedidor
{
    // El arrendador lee su propio sub-medidor y cobra el consumo al inquilino.
    PorLectura,

    // Llega la factura de la empresa (planilla) y se reparte entre los inquilinos.
    PorPlanilla
}
