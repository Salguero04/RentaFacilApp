namespace RentaFacil.Shared.Models;

public record LoginDto(string NombreUsuario, string Password);

public record LoginResultDto(string Token, string NombreUsuario, string Rol, DateTime ExpiraEn);

public record RegistrarUsuarioDto(string NombreUsuario, string Password, string Rol);

public record LoginGoogleDto(string IdToken);

// Email opcional: habilita la recuperación de contraseña por correo (plan de producción, Fase 3)
public record RegistrarInquilinoDto(string Codigo, string NombreUsuario, string Password, string? Email);
