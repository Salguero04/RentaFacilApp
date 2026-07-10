using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/medidores")]
[Authorize(Roles = RentaFacil.Shared.AppRoles.Administrador + "," + RentaFacil.Shared.AppRoles.Propietario)]
public class MedidoresController : ControllerBase
{
    private readonly IMedidorService _service;
    public MedidoresController(IMedidorService service) => _service = service;

    // ── Medidores ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetMedidoresAsync(User.ObtenerUsuarioId()));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearMedidorDto dto)
    {
        var res = await _service.CrearMedidorAsync(dto, User.ObtenerUsuarioId());
        if (res == null) return BadRequest(new { message = "El inmueble indicado no existe o no te pertenece." });
        return Ok(res);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearMedidorDto dto)
    {
        var ok = await _service.UpdateMedidorAsync(id, dto, User.ObtenerUsuarioId());
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteMedidorAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }

    // ── Vínculos medidor ↔ inquilino ─────────────────────────────────
    [HttpGet("{id}/inquilinos")]
    public async Task<IActionResult> GetVinculos(int id) => Ok(await _service.GetVinculosAsync(id, User.ObtenerUsuarioId()));

    [HttpPost("inquilinos")]
    public async Task<IActionResult> Vincular([FromBody] CrearMedidorInquilinoDto dto)
    {
        var res = await _service.VincularInquilinoAsync(dto, User.ObtenerUsuarioId());
        if (res == null) return BadRequest(new { message = "El medidor o el inquilino no existen o no te pertenecen." });
        return Ok(res);
    }

    [HttpPut("inquilinos/{id}")]
    public async Task<IActionResult> ActualizarVinculo(int id, [FromBody] CrearMedidorInquilinoDto dto)
    {
        var ok = await _service.ActualizarVinculoAsync(id, dto, User.ObtenerUsuarioId());
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("inquilinos/{id}")]
    public async Task<IActionResult> Desvincular(int id)
    {
        await _service.DesvincularAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }

    // ── Facturas / planillas ─────────────────────────────────────────
    [HttpGet("{id}/facturas")]
    public async Task<IActionResult> GetFacturas(int id) => Ok(await _service.GetFacturasAsync(id, User.ObtenerUsuarioId()));

    [HttpPost("facturas")]
    public async Task<IActionResult> GuardarFactura([FromBody] CrearFacturaMedidorDto dto)
    {
        var res = await _service.GuardarFacturaAsync(dto, User.ObtenerUsuarioId());
        if (res == null) return BadRequest(new { message = "El medidor indicado no existe o no te pertenece." });
        return Ok(res);
    }

    [HttpDelete("facturas/{id}")]
    public async Task<IActionResult> DeleteFactura(int id)
    {
        await _service.DeleteFacturaAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }

    // ── Reporte y cobros ─────────────────────────────────────────────
    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen([FromQuery] int mes, [FromQuery] int anio)
        => Ok(await _service.CalcularResumenAsync(User.ObtenerUsuarioId(), mes, anio));

    [HttpGet("cobros")]
    public async Task<IActionResult> GetCobros([FromQuery] int contratoId, [FromQuery] int mes, [FromQuery] int anio)
        => Ok(await _service.CalcularCobrosContratoAsync(User.ObtenerUsuarioId(), contratoId, mes, anio));
}
