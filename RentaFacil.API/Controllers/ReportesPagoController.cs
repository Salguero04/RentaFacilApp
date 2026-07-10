using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Controllers;

// Bandeja del arrendador para los reportes de pago ("ya pagué") que le envían sus inquilinos
// desde el portal (api/mi/reportes-pago). Confirmar NO crea el Pago automáticamente: el
// arrendador lo sigue registrando en CrearPago como siempre.
[ApiController]
[Route("api/reportes-pago")]
[Authorize(Roles = RentaFacil.Shared.AppRoles.Administrador + "," + RentaFacil.Shared.AppRoles.Propietario)]
public class ReportesPagoController : ControllerBase
{
    private readonly IReportePagoService _service;

    public ReportesPagoController(IReportePagoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetBandeja([FromQuery] EstadoReportePago? estado) =>
        Ok(await _service.GetBandejaAsync(User.ObtenerUsuarioId(), estado));

    [HttpGet("{id}/comprobante")]
    public async Task<IActionResult> GetComprobante(int id)
    {
        var foto = await _service.GetComprobanteAsync(id, User.ObtenerUsuarioId());
        if (foto == null) return NotFound();
        return File(foto, "image/jpeg");
    }

    [HttpPut("{id}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var actualizado = await _service.ConfirmarAsync(id, User.ObtenerUsuarioId());
        if (!actualizado) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}/rechazar")]
    public async Task<IActionResult> Rechazar(int id)
    {
        var actualizado = await _service.RechazarAsync(id, User.ObtenerUsuarioId());
        if (!actualizado) return NotFound();
        return NoContent();
    }
}
