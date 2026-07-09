using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("auth")]
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

    [HttpPost("login-google")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginGoogle([FromBody] LoginGoogleDto dto)
    {
        var resultado = await _service.LoginGoogleAsync(dto);
        if (resultado.Resultado != null) return Ok(resultado.Resultado);

        return resultado.Error switch
        {
            ErrorLoginGoogle.NoConfigurado => StatusCode(503, new { message = "El inicio de sesión con Google no está configurado en el servidor." }),
            ErrorLoginGoogle.RegistroNoPermitido => StatusCode(403, new { message = "Tu cuenta de Google no está registrada. Contacta al administrador." }),
            _ => Unauthorized()
        };
    }
}
