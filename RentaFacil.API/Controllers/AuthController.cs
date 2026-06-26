using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAutenticacionService _service;
    public AuthController(IAutenticacionService service) => _service = service;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var resultado = await _service.LoginAsync(dto);
        if (resultado == null) return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        return Ok(resultado);
    }

    [HttpPost("registrar")]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDto dto)
    {
        var usuario = await _service.RegistrarAsync(dto);
        return Ok(new { usuario.Id, usuario.NombreUsuario, usuario.Rol });
    }
}
