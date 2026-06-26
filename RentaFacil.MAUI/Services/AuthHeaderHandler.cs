using System.Net;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace RentaFacil.MAUI.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private const string TokenKey = "auth_token";
    private readonly AuthService _authService;

    public AuthHeaderHandler(AuthService authService, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await SecureStorage.GetAsync(TokenKey);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.Logout();
        }

        return response;
    }
}
