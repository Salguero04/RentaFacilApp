using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

// Portal del inquilino: vista de solo lectura de sus propios contratos/pagos/consumos/
// notificaciones, derivada de los Inquilino vinculados a su cuenta (rol Inquilino). El id de
// cuenta sale del token (User.ObtenerUsuarioId()) — para una cuenta inquilino ese claim ES su cuenta.
[ApiController]
[Route("api/mi")]
[Authorize(Roles = AppRoles.Inquilino)]
public class MiPortalController : ControllerBase
{
    private readonly IPortalInquilinoService _service;
    private readonly IVinculacionService _vinculacionService;
    private readonly IReportePagoService _reportePagoService;

    public MiPortalController(IPortalInquilinoService service, IVinculacionService vinculacionService, IReportePagoService reportePagoService)
    {
        _service = service;
        _vinculacionService = vinculacionService;
        _reportePagoService = reportePagoService;
    }

    [HttpGet("contratos")]
    public async Task<IActionResult> GetContratos() => Ok(await _service.GetContratosAsync(User.ObtenerUsuarioId()));

    [HttpGet("pagos")]
    public async Task<IActionResult> GetPagos() => Ok(await _service.GetPagosAsync(User.ObtenerUsuarioId()));

    [HttpGet("pagos/{id}/recibo")]
    public async Task<IActionResult> GetReciboPago(int id, [FromQuery] string formato = "carta")
    {
        var pdfBytes = await _service.GetReciboPagoAsync(id, User.ObtenerUsuarioId(), formato);
        if (pdfBytes == null) return NotFound();
        return File(pdfBytes, "application/pdf", $"Recibo_Pago_{id}.pdf");
    }

    [HttpGet("consumos")]
    public async Task<IActionResult> GetConsumos() => Ok(await _service.GetConsumosAsync(User.ObtenerUsuarioId()));

    [HttpGet("notificaciones")]
    public async Task<IActionResult> GetNotificaciones() => Ok(await _service.GetNotificacionesAsync(User.ObtenerUsuarioId()));

    [HttpPut("notificaciones/{id}/leida")]
    public async Task<IActionResult> MarcarNotificacionLeida(int id)
    {
        var actualizado = await _service.MarcarNotificacionLeidaAsync(id, User.ObtenerUsuarioId());
        if (!actualizado) return NotFound();
        return NoContent();
    }

    [HttpPost("vincular")]
    public async Task<IActionResult> Vincular([FromBody] VincularCodigoDto dto)
    {
        var vinculado = await _vinculacionService.VincularCuentaExistenteAsync(dto.Codigo, User.ObtenerUsuarioId());
        if (!vinculado) return NotFound();
        return NoContent();
    }

    [HttpPost("reportes-pago")]
    public async Task<IActionResult> CrearReportePago([FromBody] CrearReportePagoDto dto)
    {
        var creado = await _reportePagoService.CrearAsync(dto, User.ObtenerUsuarioId());
        if (creado == null) return BadRequest();
        return Created($"api/mi/reportes-pago/{creado.Id}", creado);
    }

    [HttpGet("reportes-pago")]
    public async Task<IActionResult> GetReportesPago() => Ok(await _reportePagoService.GetMisReportesAsync(User.ObtenerUsuarioId()));
}
