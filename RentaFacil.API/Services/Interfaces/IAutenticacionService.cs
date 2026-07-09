using RentaFacil.API.Models;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

/// <summary>Motivo por el cual un login con Google no devolvió un token.</summary>
public enum ErrorLoginGoogle
{
    NoConfigurado,
    TokenInvalido,
    RegistroNoPermitido,
    CredencialesInvalidas
}

/// <summary>Resultado de un intento de login con Google: o hay Resultado, o hay Error (nunca ambos).</summary>
public record ResultadoLoginGoogle(LoginResultDto? Resultado, ErrorLoginGoogle? Error);

public interface IAutenticacionService
{
    Task<LoginResultDto?> LoginAsync(LoginDto dto);
    Task<Usuario> RegistrarAsync(RegistrarUsuarioDto dto);
    Task<ResultadoLoginGoogle> LoginGoogleAsync(LoginGoogleDto dto);
}
