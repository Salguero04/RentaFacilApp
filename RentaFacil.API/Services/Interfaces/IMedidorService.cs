using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IMedidorService
{
    // Medidores
    Task<IEnumerable<MedidorDto>> GetMedidoresAsync(int usuarioId);
    Task<MedidorDto?> CrearMedidorAsync(CrearMedidorDto dto, int usuarioId);
    Task<bool> UpdateMedidorAsync(int id, CrearMedidorDto dto, int usuarioId);
    Task DeleteMedidorAsync(int id, int usuarioId);

    // Vínculos medidor ↔ inquilino
    Task<IEnumerable<MedidorInquilinoDto>> GetVinculosAsync(int medidorId, int usuarioId);
    Task<MedidorInquilinoDto?> VincularInquilinoAsync(CrearMedidorInquilinoDto dto, int usuarioId);
    Task DesvincularAsync(int vinculoId, int usuarioId);

    // Facturas / planillas
    Task<IEnumerable<FacturaMedidorDto>> GetFacturasAsync(int medidorId, int usuarioId);
    Task<FacturaMedidorDto?> GuardarFacturaAsync(CrearFacturaMedidorDto dto, int usuarioId); // upsert por medidor+mes+año
    Task DeleteFacturaAsync(int id, int usuarioId);

    // Reporte (Ingresos) y cobro por contrato (CrearPago)
    Task<IEnumerable<ResumenMedidorDto>> CalcularResumenAsync(int usuarioId, int mes, int anio);
    Task<IEnumerable<DetalleServicioPagoDto>> CalcularCobrosContratoAsync(int usuarioId, int contratoId, int mes, int anio);
}
