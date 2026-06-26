using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Extensions;
using RentaFacil.API.Models;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContratosController : ControllerBase
{
    private readonly IContratoService _service;
    public ContratosController(IContratoService service) => _service = service;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var res = await _service.GetByIdAsync(id); return res == null ? NotFound() : Ok(res); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CrearContratoDto dto) { var res = await _service.CrearAsync(dto); return CreatedAtAction(nameof(GetById), new { id = res.Id }, res); }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CrearContratoDto dto) { await _service.UpdateAsync(id, dto); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id); return NoContent(); }
}

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _service;
    private readonly IReciboService _reciboService;

    public PagosController(IPagoService service, IReciboService reciboService)
    {
        _service = service;
        _reciboService = reciboService;
    }

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var res = await _service.GetByIdAsync(id); return res == null ? NotFound() : Ok(res); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CrearPagoDto dto) { var res = await _service.CrearAsync(dto); return CreatedAtAction(nameof(GetById), new { id = res.Id }, res); }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CrearPagoDto dto) { await _service.UpdateAsync(id, dto); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id); return NoContent(); }

    [HttpGet("{id}/recibo/{formato}")]
    public async Task<IActionResult> GetRecibo(int id, string formato)
    {
        try
        {
            var pdfBytes = await _reciboService.GenerarReciboPdfAsync(id, User.ObtenerUsuarioId(), formato);
            return File(pdfBytes, "application/pdf", $"Recibo_Pago_{id}.pdf");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class UnidadesController : ControllerBase
{
    private readonly IUnidadService _service;
    public UnidadesController(IUnidadService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var unidades = await _service.GetAllAsync(User.ObtenerUsuarioId());
        return Ok(unidades);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearUnidadDto dto)
    {
        var result = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        if (result == null) return BadRequest(new { message = "El inmueble indicado no existe o no te pertenece." });
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearUnidadDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        if (!actualizado) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }
}
