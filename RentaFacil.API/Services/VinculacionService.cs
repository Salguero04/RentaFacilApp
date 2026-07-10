using QRCoder;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class VinculacionService : IVinculacionService
{
    // Sin caracteres ambiguos (0/O, 1/I/L) para que el código se pueda transcribir a mano
    // si el QR no se puede escanear.
    private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int LongitudCodigo = 8;
    private const int DiasVigencia = 7;

    private readonly ICodigoVinculacionRepository _codigoRepository;
    private readonly IContratoRepository _contratoRepository;
    private readonly IInquilinoRepository _inquilinoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAutenticacionService _autenticacionService;

    public VinculacionService(
        ICodigoVinculacionRepository codigoRepository,
        IContratoRepository contratoRepository,
        IInquilinoRepository inquilinoRepository,
        IUsuarioRepository usuarioRepository,
        IAutenticacionService autenticacionService)
    {
        _codigoRepository = codigoRepository;
        _contratoRepository = contratoRepository;
        _inquilinoRepository = inquilinoRepository;
        _usuarioRepository = usuarioRepository;
        _autenticacionService = autenticacionService;
    }

    public async Task<CodigoVinculacionDto?> GenerarCodigoAsync(int contratoId, int usuarioId)
    {
        var contrato = await _contratoRepository.GetByIdAsync(contratoId, usuarioId);
        if (contrato == null)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;
        var codigoGenerado = new CodigoVinculacion
        {
            Codigo = await GenerarCodigoUnicoAsync(),
            ContratoId = contrato.Id,
            InquilinoId = contrato.InquilinoId,
            UsuarioId = usuarioId,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddDays(DiasVigencia),
            UsadoEn = null
        };

        await _codigoRepository.AddAsync(codigoGenerado);

        return new CodigoVinculacionDto(codigoGenerado.Codigo, codigoGenerado.FechaExpiracion);
    }

    public async Task<(LoginResultDto? Resultado, string? Error)> RegistrarInquilinoAsync(RegistrarInquilinoDto dto)
    {
        var codigo = await _codigoRepository.GetVigenteAsync(dto.Codigo);
        if (codigo == null)
        {
            return (null, "El código no es válido, ya expiró o ya fue usado.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            return (null, "La contraseña debe tener al menos 8 caracteres.");
        }

        if (await _usuarioRepository.GetByNombreUsuarioAsync(dto.NombreUsuario) != null)
        {
            return (null, "Ese nombre de usuario ya está en uso.");
        }

        // El código guarda el UsuarioId del arrendador que lo generó: lo usamos para leer el
        // Inquilino con ownership correcto, sin depender de un UsuarioId de arrendador que
        // quien registra la cuenta (el inquilino) no tiene.
        var inquilino = await _inquilinoRepository.GetByIdAsync(codigo.InquilinoId, codigo.UsuarioId);
        if (inquilino == null)
        {
            return (null, "El código no es válido, ya expiró o ya fue usado.");
        }

        if (inquilino.UsuarioCuentaId != null)
        {
            return (null, "Ese inquilino ya está vinculado a otra cuenta.");
        }

        var nuevoUsuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = AppRoles.Inquilino,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var usuarioCreado = await _usuarioRepository.AddAsync(nuevoUsuario);

        inquilino.UsuarioCuentaId = usuarioCreado.Id;
        await _inquilinoRepository.UpdateAsync(inquilino);

        codigo.UsadoEn = DateTime.UtcNow;
        await _codigoRepository.UpdateAsync(codigo);

        return (_autenticacionService.EmitirToken(usuarioCreado), null);
    }

    public async Task<bool> VincularCuentaExistenteAsync(string codigo, int cuentaId)
    {
        var codigoVinculacion = await _codigoRepository.GetVigenteAsync(codigo);
        if (codigoVinculacion == null)
        {
            return false;
        }

        var inquilino = await _inquilinoRepository.GetByIdAsync(codigoVinculacion.InquilinoId, codigoVinculacion.UsuarioId);
        if (inquilino == null || inquilino.UsuarioCuentaId != null)
        {
            return false;
        }

        inquilino.UsuarioCuentaId = cuentaId;
        await _inquilinoRepository.UpdateAsync(inquilino);

        codigoVinculacion.UsadoEn = DateTime.UtcNow;
        await _codigoRepository.UpdateAsync(codigoVinculacion);

        return true;
    }

    public byte[] GenerarQrPng(string codigo)
    {
        using var generador = new QRCodeGenerator();
        using var datosQr = generador.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.M);
        using var qrPng = new PngByteQRCode(datosQr);
        return qrPng.GetGraphic(20);
    }

    private async Task<string> GenerarCodigoUnicoAsync()
    {
        string codigo;
        do
        {
            codigo = GenerarCodigoAleatorio();
        }
        while (await _codigoRepository.GetVigenteAsync(codigo) != null);

        return codigo;
    }

    private static string GenerarCodigoAleatorio()
    {
        Span<char> buffer = stackalloc char[LongitudCodigo];
        for (var i = 0; i < LongitudCodigo; i++)
        {
            buffer[i] = AlfabetoCodigo[Random.Shared.Next(AlfabetoCodigo.Length)];
        }

        return new string(buffer);
    }
}
