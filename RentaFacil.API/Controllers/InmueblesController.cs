using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InmueblesController : ControllerBase
{
    private readonly IInmuebleService _service;

    public InmueblesController(IInmuebleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inmuebles = await _service.GetAllAsync(User.ObtenerUsuarioId());
        return Ok(inmuebles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inmueble = await _service.GetByIdAsync(id, User.ObtenerUsuarioId());
        if (inmueble == null) return NotFound();
        return Ok(inmueble);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearInmuebleDto dto)
    {
        var result = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearInmuebleDto dto)
    {
        await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }
}
