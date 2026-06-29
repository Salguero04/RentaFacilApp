using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IMedidorRepository
{
    Task<IEnumerable<Medidor>> GetAllAsync(int usuarioId);          // incluye Inmueble + Inquilinos
    Task<Medidor?> GetByIdAsync(int id, int usuarioId);
    Task<Medidor> AddAsync(Medidor medidor);
    Task UpdateAsync(Medidor medidor);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IMedidorInquilinoRepository
{
    Task<IEnumerable<MedidorInquilino>> GetByMedidorAsync(int medidorId, int usuarioId);   // incluye Inquilino
    Task<IEnumerable<MedidorInquilino>> GetByContratoAsync(int contratoId, int usuarioId); // incluye Medidor
    Task<MedidorInquilino?> GetByIdAsync(int id, int usuarioId);
    Task<MedidorInquilino> AddAsync(MedidorInquilino vinculo);
    Task UpdateAsync(MedidorInquilino vinculo);
    Task DeleteAsync(int id, int usuarioId);
}

public interface IFacturaMedidorRepository
{
    Task<IEnumerable<FacturaMedidor>> GetAllAsync(int usuarioId);
    Task<IEnumerable<FacturaMedidor>> GetByMedidorAsync(int medidorId, int usuarioId);
    Task<FacturaMedidor?> GetByClaveAsync(int medidorId, int mes, int anio, int usuarioId);
    Task<FacturaMedidor> AddAsync(FacturaMedidor factura);
    Task UpdateAsync(FacturaMedidor factura);
    Task DeleteAsync(int id, int usuarioId);
}

public interface INotificacionPendienteRepository
{
    Task<IEnumerable<NotificacionPendiente>> GetAllAsync(int usuarioId);
    Task<NotificacionPendiente> AddAsync(NotificacionPendiente notificacion);
}
