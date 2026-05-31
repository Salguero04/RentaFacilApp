using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InquilinosController : ControllerBase
{
    private readonly IInquilinoService _service;

    public InquilinosController(IInquilinoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inquilinos = await _service.GetAllAsync();
        return Ok(inquilinos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inquilino = await _service.GetByIdAsync(id);
        if (inquilino == null) return NotFound();
        return Ok(inquilino);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearInquilinoDto dto)
    {
        var result = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearInquilinoDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
