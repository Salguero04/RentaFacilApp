using RentaFacil.UI.Abstractions;

namespace RentaFacil.UI.Services;

/// <summary>
/// Implementación por defecto de <see cref="IProveedorGoogle"/> mientras no exista
/// una implementación real por plataforma. Reporta que Google login no está soportado.
/// </summary>
public class ProveedorGoogleNoSoportado : IProveedorGoogle
{
    public bool EstaSoportado => false;

    public Task<string?> ObtenerIdTokenAsync() => Task.FromResult<string?>(null);
}
