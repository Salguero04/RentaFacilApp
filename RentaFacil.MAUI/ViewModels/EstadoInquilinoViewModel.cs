using RentaFacil.Shared.Models;

namespace RentaFacil.MAUI.ViewModels;

public class EstadoInquilinoViewModel
{
    public InquilinoDto Inquilino { get; set; } = null!;
    public ContratoDto Contrato { get; set; } = null!;
    public UnidadDto Unidad { get; set; } = null!;
    
    // Pago info (del mes actual)
    public PagoDto? PagoActual { get; set; }
    
    public bool HaPagado => PagoActual?.Completado ?? false;
    public decimal SaldoPendiente => PagoActual == null
        ? Contrato.Monto
        : Math.Max(0, (PagoActual.TotalMonto + PagoActual.Servicios) - PagoActual.ACuenta);
    public bool RequiereFactura { get; set; } // Puede setearse desde el Contrato o Pago
    public DateTime FechaFiltro { get; set; } = DateTime.Today;
    public string PeriodoActual => PagoActual?.Periodo ?? $"{FechaFiltro.ToString("MMM").ToUpper()}-{FechaFiltro.AddMonths(1).ToString("MMM").ToUpper()}/{FechaFiltro.ToString("yy")}";
}
