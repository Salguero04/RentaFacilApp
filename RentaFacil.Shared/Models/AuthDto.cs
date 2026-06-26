namespace RentaFacil.Shared.Models;

public record LoginDto(string NombreUsuario, string Password);

public record LoginResultDto(string Token, string NombreUsuario, string Rol, DateTime ExpiraEn);

public record RegistrarUsuarioDto(string NombreUsuario, string Password, string Rol);
