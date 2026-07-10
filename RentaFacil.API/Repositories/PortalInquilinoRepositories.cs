using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class CodigoVinculacionRepository : ICodigoVinculacionRepository
{
    private readonly AppDbContext _context;
    public CodigoVinculacionRepository(AppDbContext context) => _context = context;

    public async Task<CodigoVinculacion> AddAsync(CodigoVinculacion codigo)
    {
        _context.CodigosVinculacion.Add(codigo);
        await _context.SaveChangesAsync();
        return codigo;
    }

    public async Task<CodigoVinculacion?> GetVigenteAsync(string codigo) =>
        await _context.CodigosVinculacion.FirstOrDefaultAsync(c =>
            c.Codigo == codigo && c.UsadoEn == null && c.FechaExpiracion > DateTime.UtcNow);

    public async Task UpdateAsync(CodigoVinculacion codigo)
    {
        _context.CodigosVinculacion.Update(codigo);
        await _context.SaveChangesAsync();
    }
}

public class ReportePagoRepository : IReportePagoRepository
{
    private readonly AppDbContext _context;
    public ReportePagoRepository(AppDbContext context) => _context = context;

    public async Task<ReportePago> AddAsync(ReportePago reporte)
    {
        _context.ReportesPago.Add(reporte);
        await _context.SaveChangesAsync();
        return reporte;
    }

    public async Task<IEnumerable<ReportePago>> GetByArrendadorAsync(int usuarioId) =>
        await _context.ReportesPago.Where(r => r.UsuarioId == usuarioId).ToListAsync();

    public async Task<IEnumerable<ReportePago>> GetByCuentaInquilinoAsync(int cuentaInquilinoId) =>
        await _context.ReportesPago.Where(r => r.CuentaInquilinoId == cuentaInquilinoId).ToListAsync();

    public async Task<ReportePago?> GetByIdAsync(int id, int usuarioId) =>
        await _context.ReportesPago.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

    public async Task UpdateAsync(ReportePago reporte)
    {
        _context.ReportesPago.Update(reporte);
        await _context.SaveChangesAsync();
    }
}

// OJO — seguridad: a diferencia del resto de repos de renta.*, estos métodos NO filtran por
// UsuarioId del arrendador. Su seguridad es la lista de inquilinoIds/cuentaId que el Service
// (PortalInquilinoService) deriva de la cuenta autenticada (token) ANTES de llamar aquí —
// no usar este repositorio directo desde un Controller.
public class PortalInquilinoRepository : IPortalInquilinoRepository
{
    private readonly AppDbContext _context;
    public PortalInquilinoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Inquilino>> GetInquilinosPorCuentaAsync(int cuentaId) =>
        await _context.Inquilinos.Where(i => i.UsuarioCuentaId == cuentaId).ToListAsync();

    public async Task<IEnumerable<Contrato>> GetContratosPorInquilinosAsync(List<int> inquilinoIds) =>
        await _context.Contratos.Where(c => inquilinoIds.Contains(c.InquilinoId)).ToListAsync();

    public async Task<IEnumerable<Pago>> GetPagosPorContratosAsync(List<int> contratoIds) =>
        await _context.Pagos.Where(p => contratoIds.Contains(p.ContratoId)).ToListAsync();

    public async Task<IEnumerable<MedidorInquilino>> GetVinculosMedidorPorInquilinosAsync(List<int> inquilinoIds) =>
        await _context.MedidoresInquilino
            .Include(v => v.Medidor)
            .Where(v => inquilinoIds.Contains(v.InquilinoId))
            .ToListAsync();

    public async Task<IEnumerable<NotificacionPendiente>> GetNotificacionesPorInquilinosAsync(List<int> inquilinoIds) =>
        await _context.NotificacionesPendientes.Where(n => inquilinoIds.Contains(n.InquilinoId)).ToListAsync();

    public async Task<NotificacionPendiente?> GetNotificacionAsync(int id) =>
        await _context.NotificacionesPendientes.FirstOrDefaultAsync(n => n.Id == id);

    public async Task MarcarNotificadaAsync(NotificacionPendiente notificacion)
    {
        _context.NotificacionesPendientes.Update(notificacion);
        await _context.SaveChangesAsync();
    }
}
