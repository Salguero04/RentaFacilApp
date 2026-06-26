using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _repository;
    private readonly IConfiguration _configuration;

    public AutenticacionService(IUsuarioRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    public async Task<LoginResultDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _repository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (usuario == null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return null;
        }

        var expiraEn = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: expiraEn, signingCredentials: credenciales);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResultDto(tokenString, usuario.NombreUsuario, usuario.Rol, expiraEn);
    }

    public async Task<Usuario> RegistrarAsync(RegistrarUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        return await _repository.AddAsync(usuario);
    }
}
