using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

// Vinculación de una cuenta de inquilino (auth.Usuarios, rol Inquilino) a un Inquilino de
// renta.* vía código de un solo uso generado por el arrendador (se distribuye como QR).
public interface IVinculacionService
{
    // Arrendador: genera código para un contrato suyo (null si el contrato no es suyo).
    Task<CodigoVinculacionDto?> GenerarCodigoAsync(int contratoId, int usuarioId);

    // Público: crea cuenta rol Inquilino y vincula. Errores tipados para el controller.
    Task<(LoginResultDto? Resultado, string? Error)> RegistrarInquilinoAsync(RegistrarInquilinoDto dto);

    // Inquilino ya logueado que agrega otro contrato/arrendador con un código nuevo.
    Task<bool> VincularCuentaExistenteAsync(string codigo, int cuentaId);

    byte[] GenerarQrPng(string codigo);
}
