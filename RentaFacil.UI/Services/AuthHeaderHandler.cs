using System.Net;
using System.Net.Http.Headers;
using RentaFacil.UI.Abstractions;

namespace RentaFacil.UI.Services;

/// <summary>
/// Adjunta el token JWT (si existe) a cada petición y, ante un 401,
/// limpia el token almacenado. Depende solo de <see cref="ITokenStore"/>
/// (no de <c>AuthService</c>) para evitar una dependencia circular con el HttpClient.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public AuthHeaderHandler(ITokenStore tokenStore, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token inválido/expirado: lo limpiamos. La próxima navegación mostrará el login.
            await _tokenStore.ClearAsync();
        }

        return response;
    }
}
