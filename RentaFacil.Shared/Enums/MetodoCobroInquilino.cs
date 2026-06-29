namespace RentaFacil.Shared.Enums;

// Cómo se obtiene el monto que paga un inquilino vinculado a un medidor.
public enum MetodoCobroInquilino
{
    // (LecturaActual − LecturaAnterior) × Tarifa del medidor.
    Tarifa,

    // Parte de la factura del medidor, proporcional al consumo (o partes iguales).
    Prorrateo,

    // Monto fijo/manual ingresado por el arrendador.
    Manual
}
