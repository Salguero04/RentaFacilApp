using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly IValidadorTokenGoogle _validadorTokenGoogle;

    public AutenticacionService(IUsuarioRepository repository, IConfiguration configuration, IValidadorTokenGoogle validadorTokenGoogle)
    {
        _repository = repository;
        _configuration = configuration;
        _validadorTokenGoogle = validadorTokenGoogle;
    }

    public async Task<LoginResultDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _repository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (usuario == null || !usuario.Activo || usuario.PasswordHash == null
            || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return null;
        }

        return GenerarToken(usuario);
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

    public async Task<ResultadoLoginGoogle> LoginGoogleAsync(LoginGoogleDto dto)
    {
        if (!_validadorTokenGoogle.EstaConfigurado)
        {
            return new ResultadoLoginGoogle(null, ErrorLoginGoogle.NoConfigurado);
        }

        var info = await _validadorTokenGoogle.ValidarAsync(dto.IdToken);
        if (info == null)
        {
            return new ResultadoLoginGoogle(null, ErrorLoginGoogle.TokenInvalido);
        }

        var usuarioPorGoogleId = await _repository.GetByGoogleIdAsync(info.GoogleId);
        if (usuarioPorGoogleId != null)
        {
            if (!usuarioPorGoogleId.Activo)
            {
                return new ResultadoLoginGoogle(null, ErrorLoginGoogle.CredencialesInvalidas);
            }

            return new ResultadoLoginGoogle(GenerarToken(usuarioPorGoogleId), null);
        }

        var usuarioPorEmail = await _repository.GetByEmailAsync(info.Email);
        if (usuarioPorEmail != null && usuarioPorEmail.Activo)
        {
            usuarioPorEmail.GoogleId = info.GoogleId;
            await _repository.UpdateAsync(usuarioPorEmail);
            return new ResultadoLoginGoogle(GenerarToken(usuarioPorEmail), null);
        }

        bool.TryParse(_configuration["Google:PermitirRegistro"], out var permitirRegistro);
        if (!permitirRegistro)
        {
            return new ResultadoLoginGoogle(null, ErrorLoginGoogle.RegistroNoPermitido);
        }

        var nuevoUsuario = new Usuario
        {
            NombreUsuario = info.Email.Length > 50 ? info.Email[..50] : info.Email,
            Email = info.Email,
            GoogleId = info.GoogleId,
            PasswordHash = null,
            Rol = AppRoles.Propietario,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        await _repository.AddAsync(nuevoUsuario);

        return new ResultadoLoginGoogle(GenerarToken(nuevoUsuario), null);
    }

    private LoginResultDto GenerarToken(Usuario usuario)
    {
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
}
