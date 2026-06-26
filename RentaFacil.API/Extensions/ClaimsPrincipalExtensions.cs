using System.Security.Claims;

namespace RentaFacil.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        if (valor == null || !int.TryParse(valor, out var id))
        {
            throw new InvalidOperationException("El usuario autenticado no tiene un UsuarioId válido en el token.");
        }
        return id;
    }
}
