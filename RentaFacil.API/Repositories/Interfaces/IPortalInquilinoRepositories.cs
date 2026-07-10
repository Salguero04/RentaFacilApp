using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface ICodigoVinculacionRepository
{
    Task<CodigoVinculacion> AddAsync(CodigoVinculacion codigo);
    Task<CodigoVinculacion?> GetVigenteAsync(string codigo);      // no usado y no expirado
    Task UpdateAsync(CodigoVinculacion codigo);

    // Reclamo atómico: marca UsadoEn en una sola sentencia condicional (UPDATE ... WHERE UsadoEn
    // IS NULL) para cerrar la ventana TOCTOU entre "verificar vigente" y "marcar usado" cuando dos
    // requests concurrentes intentan consumir el mismo código. Devuelve false si el código ya no
    // estaba disponible (ya usado o expirado) en el momento del reclamo.
    Task<bool> ReclamarAsync(int id);

    // Busca en TODOS los códigos (usados, expirados o vigentes) — el índice único de `Codigo` es
    // global, así que el chequeo de colisión al generar uno nuevo debe serlo también.
    Task<bool> ExisteAsync(string codigo);
}

public interface IReportePagoRepository
{
    Task<ReportePago> AddAsync(ReportePago reporte);
    Task<IEnumerable<ReportePago>> GetByArrendadorAsync(int usuarioId);
    Task<IEnumerable<ReportePago>> GetByCuentaInquilinoAsync(int cuentaInquilinoId);
    Task<ReportePago?> GetByIdAsync(int id, int usuarioId);        // ownership arrendador
    Task UpdateAsync(ReportePago reporte);
}

// OJO — seguridad de este repositorio: a diferencia del resto de repos de renta.*, estos
// métodos NO filtran por UsuarioId del arrendador (un inquilino puede estar vinculado a un
// solo arrendador, pero la cuenta del inquilino no tiene un UsuarioId de arrendador propio).
// La seguridad la aporta el Service, que deriva de la cuenta autenticada (token) la lista de
// inquilinoIds/cuentaId permitidos ANTES de llamar a estos métodos. No usar estos métodos
// directo desde un Controller sin pasar por ese Service.
public interface IPortalInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetInquilinosPorCuentaAsync(int cuentaId);        // UsuarioCuentaId == cuentaId
    Task<IEnumerable<Contrato>> GetContratosPorInquilinosAsync(List<int> inquilinoIds);
    Task<IEnumerable<Pago>> GetPagosPorContratosAsync(List<int> contratoIds);
    Task<IEnumerable<MedidorInquilino>> GetVinculosMedidorPorInquilinosAsync(List<int> inquilinoIds); // Include(Medidor)
    Task<IEnumerable<NotificacionPendiente>> GetNotificacionesPorInquilinosAsync(List<int> inquilinoIds);
    Task<NotificacionPendiente?> GetNotificacionAsync(int id);
    Task MarcarNotificadaAsync(NotificacionPendiente notificacion);
}
