using Google.Apis.Auth;
using RentaFacil.API.Services.Interfaces;

namespace RentaFacil.API.Services;

public class ValidadorTokenGoogle : IValidadorTokenGoogle
{
    private readonly IConfiguration _configuration;

    public ValidadorTokenGoogle(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Se lee en cada acceso (no se cachea en el constructor) para que la API
    // pueda arrancar sin "Google:ClientId" configurado.
    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_configuration["Google:ClientId"]);

    public async Task<GoogleTokenInfo?> ValidarAsync(string idToken)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            // Sin email (token con scope insuficiente) no sirve para este flujo: no hay con qué
            // vincular ni registrar la cuenta.
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            return new GoogleTokenInfo(payload.Subject, payload.Email, payload.Name, payload.EmailVerified);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
