# Seguridad real + Auditoría — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el login local falso de RentaFacil por autenticación real de servidor (JWT + BCrypt), cerrar el IDOR/BOLA confirmado (filtrar todo por `UsuarioId` del usuario autenticado), agregar auditoría de cambios, cabeceras de seguridad HTTP, rate limiting en login y validación de cédula/RUC ecuatoriano.

**Architecture:** `RentaFacil.API` gana una tabla `Usuarios` + JWT bearer auth; cada Repository de entidad de dominio (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`) recibe el `UsuarioId` del `ClaimsPrincipal` y filtra en el `Where`; un `SaveChangesInterceptor` sella auditoría automáticamente. `RentaFacil.MAUI` cambia su `AuthService`/`ApiClient` para hablar con la API real en vez de `Preferences` local.

**Tech Stack:** ASP.NET Core 10 JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), `BCrypt.Net-Next`, EF Core 10 (SQLite), xUnit + Moq + FluentAssertions, .NET MAUI `SecureStorage`.

## Global Constraints

- Idioma español en código, comentarios, identificadores y mensajes de error (regla del proyecto, ver `CLAUDE.md`).
- Capas `Model → Repository → Service → Controller`; el filtro por `UsuarioId` vive en el **Repository**, nunca en el Controller (spec, sección "Filtro IDOR").
- `UsuarioId` se obtiene siempre del `ClaimsPrincipal` (JWT), nunca del body del request — ningún DTO de creación vuelve a llevar `UsuarioId`.
- Todas las PK/FK del proyecto son `int`, no `long` (incluye `IAuditable`).
- JWT con expiración fija de 8 horas, sin refresh token.
- Clave de firma JWT y credenciales del usuario sembrado vienen de configuración (User Secrets en desarrollo), nunca hardcodeadas.
- No se modifica `UseHttpsRedirection()` (sigue comentado) ni la política CORS abierta — vigentes para Fase 1 LAN.
- Migraciones EF Core: `dotnet ef migrations add <Nombre> --project RentaFacil.API --startup-project RentaFacil.API` (ejecutar desde la raíz del repo).
- Spec de referencia: `docs/superpowers/specs/2026-06-26-seguridad-auditoria-design.md`.

---

### Task 1: Paquetes NuGet para auth

**Files:**
- Modify: `RentaFacil.API/RentaFacil.API.csproj`

**Interfaces:**
- Produces: paquetes `BCrypt.Net-Next` y `Microsoft.AspNetCore.Authentication.JwtBearer` disponibles para todo el proyecto API.

- [ ] **Step 1: Agregar los paquetes**

```bash
dotnet add RentaFacil.API package BCrypt.Net-Next
dotnet add RentaFacil.API package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
```

- [ ] **Step 2: Verificar que el proyecto sigue compilando**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add RentaFacil.API/RentaFacil.API.csproj
git commit -m "chore: add BCrypt and JWT bearer packages"
```

---

### Task 2: Entidad `Usuario`, `AppRoles` y `IUsuarioRepository`

**Files:**
- Create: `RentaFacil.Shared/AppRoles.cs`
- Create: `RentaFacil.API/Models/Usuario.cs`
- Create: `RentaFacil.API/Repositories/Interfaces/IUsuarioRepository.cs`
- Create: `RentaFacil.API/Repositories/UsuarioRepository.cs`
- Modify: `RentaFacil.API/Data/AppDbContext.cs`
- Create: migración EF Core `AddUsuarios`

**Interfaces:**
- Produces: `RentaFacil.Shared.AppRoles.{Administrador,Propietario,Inquilino}` (constantes `string`); `Usuario { Id, NombreUsuario, Email, PasswordHash, Rol, Activo, FechaCreacion }`; `IUsuarioRepository.{GetByNombreUsuarioAsync(string), AddAsync(Usuario), ExisteAlgunoAsync()}`.

- [ ] **Step 1: Crear `AppRoles`**

```csharp
namespace RentaFacil.Shared;

public static class AppRoles
{
    public const string Administrador = "Administrador";
    public const string Propietario = "Propietario";
    public const string Inquilino = "Inquilino";
}
```

- [ ] **Step 2: Crear el modelo `Usuario`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = null!;

    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Rol { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
}
```

- [ ] **Step 3: Agregar `DbSet<Usuario>` e índice único en `AppDbContext`**

En `RentaFacil.API/Data/AppDbContext.cs`, agregar dentro de la clase:

```csharp
public DbSet<Usuario> Usuarios { get; set; }
```

Y dentro de `OnModelCreating`, agregar:

```csharp
modelBuilder.Entity<Usuario>()
    .HasIndex(u => u.NombreUsuario)
    .IsUnique();
```

- [ ] **Step 4: Crear `IUsuarioRepository`**

```csharp
using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario> AddAsync(Usuario usuario);
    Task<bool> ExisteAlgunoAsync();
}
```

- [ ] **Step 5: Implementar `UsuarioRepository`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;
    public UsuarioRepository(AppDbContext context) => _context = context;

    public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

    public async Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<bool> ExisteAlgunoAsync() => await _context.Usuarios.AnyAsync();
}
```

- [ ] **Step 6: Generar la migración**

```bash
dotnet ef migrations add AddUsuarios --project RentaFacil.API --startup-project RentaFacil.API
```

- [ ] **Step 7: Verificar build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add RentaFacil.Shared/AppRoles.cs RentaFacil.API/Models/Usuario.cs RentaFacil.API/Repositories/Interfaces/IUsuarioRepository.cs RentaFacil.API/Repositories/UsuarioRepository.cs RentaFacil.API/Data/AppDbContext.cs RentaFacil.API/Migrations
git commit -m "feat: add Usuario entity, AppRoles, and IUsuarioRepository"
```

---

### Task 3: DTOs de autenticación

**Files:**
- Create: `RentaFacil.Shared/Models/AuthDto.cs`

**Interfaces:**
- Consumes: nada.
- Produces: `LoginDto(NombreUsuario, Password)`, `LoginResultDto(Token, NombreUsuario, Rol, ExpiraEn)`, `RegistrarUsuarioDto(NombreUsuario, Password, Rol)`.

- [ ] **Step 1: Crear los DTOs**

```csharp
namespace RentaFacil.Shared.Models;

public record LoginDto(string NombreUsuario, string Password);

public record LoginResultDto(string Token, string NombreUsuario, string Rol, DateTime ExpiraEn);

public record RegistrarUsuarioDto(string NombreUsuario, string Password, string Rol);
```

- [ ] **Step 2: Verificar build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add RentaFacil.Shared/Models/AuthDto.cs
git commit -m "feat: add auth DTOs (LoginDto, LoginResultDto, RegistrarUsuarioDto)"
```

---

### Task 4: `IAutenticacionService` / `AutenticacionService` con tests

**Files:**
- Create: `RentaFacil.API/Services/Interfaces/IAutenticacionService.cs`
- Create: `RentaFacil.API/Services/AutenticacionService.cs`
- Create: `RentaFacil.Tests/AutenticacionServiceTests.cs`

**Interfaces:**
- Consumes: `IUsuarioRepository` (Task 2), `LoginDto`/`LoginResultDto`/`RegistrarUsuarioDto` (Task 3), `AppRoles` (Task 2).
- Produces: `IAutenticacionService.{LoginAsync(LoginDto): Task<LoginResultDto?>, RegistrarAsync(RegistrarUsuarioDto): Task<Usuario>}`.

- [ ] **Step 1: Escribir los tests que fallan**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class AutenticacionServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AutenticacionService _service;

    public AutenticacionServiceTests()
    {
        _repositoryMock = new Mock<IUsuarioRepository>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("clave-de-prueba-suficientemente-larga-1234567890");
        _service = new AutenticacionService(_repositoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveToken()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = true };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave123"));

        resultado.Should().NotBeNull();
        resultado!.NombreUsuario.Should().Be("dueno");
        resultado.Rol.Should().Be(AppRoles.Administrador);
        resultado.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ConPasswordIncorrecta_DevuelveNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = true };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave-equivocada"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("fantasma")).ReturnsAsync((Usuario?)null);

        var resultado = await _service.LoginAsync(new LoginDto("fantasma", "clave123"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInactivo_DevuelveNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("clave123");
        var usuario = new Usuario { Id = 1, NombreUsuario = "dueno", PasswordHash = hash, Rol = AppRoles.Administrador, Activo = false };
        _repositoryMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);

        var resultado = await _service.LoginAsync(new LoginDto("dueno", "clave123"));

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task RegistrarAsync_HasheaLaPasswordAntesDeGuardar()
    {
        Usuario? guardado = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => guardado = u)
            .ReturnsAsync((Usuario u) => u);

        await _service.RegistrarAsync(new RegistrarUsuarioDto("nuevo", "clave123", AppRoles.Propietario));

        guardado.Should().NotBeNull();
        guardado!.PasswordHash.Should().NotBe("clave123");
        BCrypt.Net.BCrypt.Verify("clave123", guardado.PasswordHash).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Confirmar que no compila (no existe `AutenticacionService` todavía)**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~AutenticacionServiceTests"`
Expected: FAIL — error de compilación, `AutenticacionService` no existe.

- [ ] **Step 3: Crear la interfaz**

```csharp
using RentaFacil.API.Models;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IAutenticacionService
{
    Task<LoginResultDto?> LoginAsync(LoginDto dto);
    Task<Usuario> RegistrarAsync(RegistrarUsuarioDto dto);
}
```

- [ ] **Step 4: Implementar `AutenticacionService`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _repository;
    private readonly IConfiguration _configuration;

    public AutenticacionService(IUsuarioRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    public async Task<LoginResultDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _repository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (usuario == null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return null;
        }

        var expiraEn = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: expiraEn, signingCredentials: credenciales);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResultDto(tokenString, usuario.NombreUsuario, usuario.Rol, expiraEn);
    }

    public async Task<Usuario> RegistrarAsync(RegistrarUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        return await _repository.AddAsync(usuario);
    }
}
```

- [ ] **Step 5: Correr los tests**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~AutenticacionServiceTests"`
Expected: PASS (5 tests)

- [ ] **Step 6: Commit**

```bash
git add RentaFacil.API/Services/Interfaces/IAutenticacionService.cs RentaFacil.API/Services/AutenticacionService.cs RentaFacil.Tests/AutenticacionServiceTests.cs
git commit -m "feat: add AutenticacionService (BCrypt + JWT)"
```

---

### Task 5: `AuthController` y wiring de JWT/Authorization en `Program.cs`

**Files:**
- Create: `RentaFacil.API/Controllers/AuthController.cs`
- Modify: `RentaFacil.API/Program.cs`

**Interfaces:**
- Consumes: `IAutenticacionService` (Task 4), `AppRoles` (Task 2).
- Produces: `POST /api/auth/login` (anónimo), `POST /api/auth/registrar` (`[Authorize(Roles = AppRoles.Administrador)]`); fallback policy que exige usuario autenticado en el resto de la API.

- [ ] **Step 1: Crear `AuthController`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAutenticacionService _service;
    public AuthController(IAutenticacionService service) => _service = service;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var resultado = await _service.LoginAsync(dto);
        if (resultado == null) return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        return Ok(resultado);
    }

    [HttpPost("registrar")]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDto dto)
    {
        var usuario = await _service.RegistrarAsync(dto);
        return Ok(new { usuario.Id, usuario.NombreUsuario, usuario.Rol });
    }
}
```

- [ ] **Step 2: Configurar User Secrets para desarrollo**

```bash
dotnet user-secrets init --project RentaFacil.API
dotnet user-secrets set "Jwt:Key" "una-clave-bien-larga-y-aleatoria-cambiame-1234567890" --project RentaFacil.API
```

- [ ] **Step 3: Modificar `Program.cs` — agregar usings, servicios y middleware de auth**

Agregar al inicio del archivo, junto a los `using` existentes:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using RentaFacil.API.Services.Interfaces;
```

Después de la línea `builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IReciboService, RentaFacil.API.Services.ReciboService>();`, agregar:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IUsuarioRepository, RentaFacil.API.Repositories.UsuarioRepository>();
builder.Services.AddScoped<IAutenticacionService, RentaFacil.API.Services.AutenticacionService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key. Ejecuta: dotnet user-secrets set \"Jwt:Key\" \"<clave>\" --project RentaFacil.API");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

- [ ] **Step 4: Activar el middleware de autenticación en el pipeline**

Reemplazar:

```csharp
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
```

Por:

```csharp
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

- [ ] **Step 5: Build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 6: Verificación manual — login funciona, endpoints protegidos rechazan sin token**

```bash
dotnet run --project RentaFacil.API
```

En otra terminal (con la API corriendo):

```bash
curl -i http://localhost:5295/api/inquilinos
```
Expected: `401 Unauthorized` (todavía no hay usuario sembrado, pero el endpoint ya exige auth).

Detener la API (Ctrl+C) antes de continuar al siguiente task — el seed se reescribe en la Task 9.

- [ ] **Step 7: Commit**

```bash
git add RentaFacil.API/Controllers/AuthController.cs RentaFacil.API/Program.cs
git commit -m "feat: add AuthController and wire JWT bearer authentication"
```

---

### Task 6: Cabeceras de seguridad HTTP

**Files:**
- Modify: `RentaFacil.API/Program.cs`

**Interfaces:**
- Produces: middleware global que agrega `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy` a toda respuesta de la API.

- [ ] **Step 1: Agregar el middleware, después de `app.UseAuthorization();` y antes de `app.MapControllers();`**

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self' https://cdn.jsdelivr.net; font-src 'self' https://cdn.jsdelivr.net";
    await next();
});
```

- [ ] **Step 2: Build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 3: Verificación manual — cabeceras presentes y Swagger UI sin errores de CSP**

```bash
dotnet run --project RentaFacil.API
```

En otra terminal:

```bash
curl -i http://localhost:5295/api/auth/login -X POST -H "Content-Type: application/json" -d "{}"
```
Expected: la respuesta incluye `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy: ...`.

Abrir `http://localhost:5295/swagger` en el navegador (entorno Development) y revisar la consola de DevTools: no debe haber errores de "Refused to ... because it violates the following Content Security Policy directive". Detener la API (Ctrl+C) al terminar.

- [ ] **Step 4: Commit**

```bash
git add RentaFacil.API/Program.cs
git commit -m "feat: add HTTP security headers middleware"
```

---

### Task 7: Rate limiting en login

**Files:**
- Modify: `RentaFacil.API/Program.cs`
- Modify: `RentaFacil.API/Controllers/AuthController.cs`

**Interfaces:**
- Consumes: `AuthController.Login` (Task 5).
- Produces: política `"auth"` de rate limiting (10 intentos/min por IP, `429` al exceder) aplicada solo a `POST /api/auth/login`.

- [ ] **Step 1: Agregar `AddRateLimiter` en `Program.cs`, después del bloque `AddAuthorization`**

Agregar el `using`:

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
```

Agregar el servicio:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, QueueLimit = 0 }));
});
```

- [ ] **Step 2: Activar el middleware, después de `app.UseAuthorization();`**

```csharp
app.UseRateLimiter();
```

- [ ] **Step 3: Anotar el endpoint de login**

En `RentaFacil.API/Controllers/AuthController.cs`, agregar el using `Microsoft.AspNetCore.RateLimiting` y decorar el método `Login`:

```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
```

- [ ] **Step 4: Build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 5: Verificación manual — el intento 11 en un minuto devuelve 429**

```bash
dotnet run --project RentaFacil.API
```

En otra terminal:

```bash
for i in $(seq 1 11); do curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5295/api/auth/login -H "Content-Type: application/json" -d '{"nombreUsuario":"x","password":"x"}'; done
```
Expected: las primeras 10 líneas son `401` (credenciales inválidas, pero no limitadas), la línea 11 es `429`. Detener la API (Ctrl+C) al terminar.

- [ ] **Step 6: Commit**

```bash
git add RentaFacil.API/Program.cs RentaFacil.API/Controllers/AuthController.cs
git commit -m "feat: add rate limiting to login endpoint"
```

---

### Task 8: `ClaimsPrincipalExtensions.ObtenerUsuarioId`

**Files:**
- Create: `RentaFacil.API/Extensions/ClaimsPrincipalExtensions.cs`
- Create: `RentaFacil.Tests/ClaimsPrincipalExtensionsTests.cs`

**Interfaces:**
- Produces: `ClaimsPrincipal.ObtenerUsuarioId(): int` — método de extensión usado por todos los Controllers de entidades de dominio para obtener el `UsuarioId` del JWT.

- [ ] **Step 1: Escribir el test que falla**

```csharp
using System.Security.Claims;
using FluentAssertions;
using RentaFacil.API.Extensions;

namespace RentaFacil.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void ObtenerUsuarioId_ConClaimValido_DevuelveElId()
    {
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        var principal = new ClaimsPrincipal(identidad);

        var resultado = principal.ObtenerUsuarioId();

        resultado.Should().Be(42);
    }

    [Fact]
    public void ObtenerUsuarioId_SinClaim_LanzaExcepcion()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var accion = () => principal.ObtenerUsuarioId();

        accion.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Confirmar que no compila**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~ClaimsPrincipalExtensionsTests"`
Expected: FAIL — `ClaimsPrincipalExtensions` no existe.

- [ ] **Step 3: Implementar la extensión**

```csharp
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
```

- [ ] **Step 4: Correr los tests**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~ClaimsPrincipalExtensionsTests"`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add RentaFacil.API/Extensions/ClaimsPrincipalExtensions.cs RentaFacil.Tests/ClaimsPrincipalExtensionsTests.cs
git commit -m "feat: add ClaimsPrincipalExtensions.ObtenerUsuarioId helper"
```

---

### Task 9: Siembra del usuario dueño + remapeo de datos existentes

**Files:**
- Modify: `RentaFacil.API/Program.cs`

**Interfaces:**
- Consumes: `IUsuarioRepository`/`Usuario` (Task 2), `AppRoles` (Task 2).
- Produces: al iniciar con `Usuarios` vacía, crea un usuario `Administrador` desde configuración y remapea `Inquilino.UsuarioId`/`Inmueble.UsuarioId` existentes al `Id` real de ese usuario (sin asumir `1`).

- [ ] **Step 1: Configurar credenciales del usuario sembrado (User Secrets)**

```bash
dotnet user-secrets set "SeedAdmin:Usuario" "duenotest" --project RentaFacil.API
dotnet user-secrets set "SeedAdmin:Password" "CambiaEstaClave123!" --project RentaFacil.API
```

- [ ] **Step 2: Reemplazar el bloque de seed completo**

Reemplazar todo el bloque `using (var scope = app.Services.CreateScope()) { ... }` al final de `Program.cs` por:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    if (!context.Usuarios.Any())
    {
        var seedUsuario = app.Configuration["SeedAdmin:Usuario"];
        var seedPassword = app.Configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(seedUsuario) || string.IsNullOrWhiteSpace(seedPassword))
        {
            throw new InvalidOperationException(
                "Falta configurar SeedAdmin:Usuario / SeedAdmin:Password. Ejecuta:\n" +
                "  dotnet user-secrets set \"SeedAdmin:Usuario\" \"<usuario>\" --project RentaFacil.API\n" +
                "  dotnet user-secrets set \"SeedAdmin:Password\" \"<password>\" --project RentaFacil.API");
        }

        var admin = new RentaFacil.API.Models.Usuario
        {
            NombreUsuario = seedUsuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            Rol = RentaFacil.Shared.AppRoles.Administrador,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        context.Usuarios.Add(admin);
        context.SaveChanges();

        // Remapea datos preexistentes (UsuarioId = 1 hardcodeado) al usuario sembrado real.
        // No-op en una base de datos nueva, ya que estas tablas estarían vacías.
        foreach (var inquilino in context.Inquilinos) inquilino.UsuarioId = admin.Id;
        foreach (var inmueble in context.Inmuebles) inmueble.UsuarioId = admin.Id;
        context.SaveChanges();

        if (!context.Inquilinos.Any())
        {
            var inq = new RentaFacil.API.Models.Inquilino { NombreCompleto = "Mario Salguero (Dummy)", Identificacion = "1234567", Telefono = "555-0100", FechaRegistro = DateTime.Now, UsuarioId = admin.Id };
            context.Inquilinos.Add(inq);
            context.SaveChanges();

            var inm = new RentaFacil.API.Models.Inmueble { Nombre = "Edificio Central", Direccion = "Av. Principal 123", Tipo = RentaFacil.Shared.Enums.TipoInmueble.Multiple, MontoRenta = 0, UsuarioId = admin.Id };
            context.Inmuebles.Add(inm);
            context.SaveChanges();

            var uni = new RentaFacil.API.Models.Unidad { Nombre = "Apt 1A", MontoRenta = 500, Ocupada = true, InmuebleId = inm.Id };
            context.Unidades.Add(uni);
            context.SaveChanges();

            var con = new RentaFacil.API.Models.Contrato { InquilinoId = inq.Id, UnidadId = uni.Id, Monto = 500, Garantia = 500, Frecuencia = RentaFacil.Shared.Enums.FrecuenciaPago.Mensual, DuracionMeses = 12, DiaPago = 5, FechaInicio = DateTime.Now, FechaFin = DateTime.Now.AddMonths(12), Activo = true };
            context.Contratos.Add(con);
            context.SaveChanges();

            var pag = new RentaFacil.API.Models.Pago { ContratoId = con.Id, TotalMonto = 500, ACuenta = 200, Servicios = 0, FechaPago = DateTime.Now, Periodo = "MAY-26", Facturado = false, Completado = false };
            context.Pagos.Add(pag);
            context.SaveChanges();
        }
    }
}
```

> Nota: el seed de `Unidad`/`Contrato`/`Pago` todavía no asigna `UsuarioId` propio — esas entidades lo ganan en las Tasks 14-16, que extenderán este mismo bloque.

- [ ] **Step 3: Build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 4: Verificación manual — login real funciona end-to-end**

```bash
rm RentaFacil.API/rentafacil.db
dotnet run --project RentaFacil.API
```

En otra terminal:

```bash
curl -s -X POST http://localhost:5295/api/auth/login -H "Content-Type: application/json" -d '{"nombreUsuario":"duenotest","password":"CambiaEstaClave123!"}'
```
Expected: `200 OK` con un JSON `{ "token": "...", "nombreUsuario": "duenotest", "rol": "Administrador", "expiraEn": "..." }`.

```bash
TOKEN=$(curl -s -X POST http://localhost:5295/api/auth/login -H "Content-Type: application/json" -d '{"nombreUsuario":"duenotest","password":"CambiaEstaClave123!"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5295/api/inquilinos
```
Expected: `200 OK` con el inquilino dummy. Detener la API (Ctrl+C) al terminar.

- [ ] **Step 5: Commit**

```bash
git add RentaFacil.API/Program.cs
git commit -m "feat: seed owner user from config and remap legacy UsuarioId data"
```

---

### Task 10: `IAuditable` en las 5 entidades + migración

**Files:**
- Create: `RentaFacil.API/Models/IAuditable.cs`
- Modify: `RentaFacil.API/Models/Inquilino.cs`
- Modify: `RentaFacil.API/Models/Inmueble.cs`
- Modify: `RentaFacil.API/Models/Unidad.cs`
- Modify: `RentaFacil.API/Models/Contrato.cs`
- Modify: `RentaFacil.API/Models/Pago.cs`
- Create: migración EF Core `AddAuditoriaColumns`

**Interfaces:**
- Produces: `IAuditable { CreadoPorId: int?, FechaCreacion: DateTime?, ModificadoPorId: int?, FechaModificacion: DateTime? }`, implementada por las 5 entidades de dominio.

- [ ] **Step 1: Crear `IAuditable`**

```csharp
namespace RentaFacil.API.Models;

public interface IAuditable
{
    int? CreadoPorId { get; set; }
    DateTime? FechaCreacion { get; set; }
    int? ModificadoPorId { get; set; }
    DateTime? FechaModificacion { get; set; }
}
```

- [ ] **Step 2: Implementar la interfaz en `Inquilino`**

En `RentaFacil.API/Models/Inquilino.cs`, cambiar `public class Inquilino` por `public class Inquilino : IAuditable` y agregar al final de la clase, antes del cierre:

```csharp
    public int? CreadoPorId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? ModificadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
```

- [ ] **Step 3: Repetir el Step 2 para `Inmueble`, `Unidad`, `Contrato` y `Pago`**

Misma firma `: IAuditable` y las mismas 4 propiedades agregadas al final de cada clase.

- [ ] **Step 4: Generar la migración**

```bash
dotnet ef migrations add AddAuditoriaColumns --project RentaFacil.API --startup-project RentaFacil.API
```

- [ ] **Step 5: Build**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add RentaFacil.API/Models RentaFacil.API/Migrations
git commit -m "feat: implement IAuditable on domain entities"
```

---

### Task 11: `AuditoriaInterceptor`

**Files:**
- Create: `RentaFacil.API/Data/AuditoriaInterceptor.cs`
- Modify: `RentaFacil.API/Program.cs`
- Create: `RentaFacil.Tests/AuditoriaInterceptorTests.cs`

**Interfaces:**
- Consumes: `IAuditable` (Task 10), `IHttpContextAccessor` (registrado en Task 5).
- Produces: `AuditoriaInterceptor : SaveChangesInterceptor` registrado en `AddDbContext`, sella auditoría en `Added`/`Modified`, no lanza excepción sin `HttpContext`.

- [ ] **Step 1: Escribir los tests que fallan**

```csharp
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using RentaFacil.API.Data;
using RentaFacil.API.Models;

namespace RentaFacil.Tests;

public class AuditoriaInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public AuditoriaInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    private AppDbContext CrearContexto()
    {
        var interceptor = new AuditoriaInterceptor(_httpContextAccessorMock.Object);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private void ConfigurarUsuarioAutenticado(int usuarioId)
    {
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) });
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
    }

    [Fact]
    public void Added_ConUsuarioAutenticado_SellaCreadoYModificado()
    {
        ConfigurarUsuarioAutenticado(7);
        using var context = CrearContexto();

        var inquilino = new Inquilino { NombreCompleto = "Test", Identificacion = "1", UsuarioId = 7, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        context.SaveChanges();

        inquilino.CreadoPorId.Should().Be(7);
        inquilino.FechaCreacion.Should().NotBeNull();
        inquilino.ModificadoPorId.Should().Be(7);
        inquilino.FechaModificacion.Should().NotBeNull();
    }

    [Fact]
    public void Modified_SoloActualizaModificado_SinTocarCreado()
    {
        ConfigurarUsuarioAutenticado(7);
        using var context = CrearContexto();
        var inquilino = new Inquilino { NombreCompleto = "Test", Identificacion = "1", UsuarioId = 7, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        context.SaveChanges();
        var fechaCreacionOriginal = inquilino.FechaCreacion;

        ConfigurarUsuarioAutenticado(9);
        inquilino.NombreCompleto = "Test Modificado";
        context.SaveChanges();

        inquilino.CreadoPorId.Should().Be(7);
        inquilino.FechaCreacion.Should().Be(fechaCreacionOriginal);
        inquilino.ModificadoPorId.Should().Be(9);
    }

    [Fact]
    public void Added_SinHttpContext_NoLanzaExcepcionYDejaCamposNulos()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        using var context = CrearContexto();

        var inquilino = new Inquilino { NombreCompleto = "Seed", Identificacion = "2", UsuarioId = 1, FechaRegistro = DateTime.Now };
        context.Inquilinos.Add(inquilino);
        var accion = () => context.SaveChanges();

        accion.Should().NotThrow();
        inquilino.CreadoPorId.Should().BeNull();
        inquilino.ModificadoPorId.Should().BeNull();
        inquilino.FechaCreacion.Should().NotBeNull();
    }

    public void Dispose() => _connection.Dispose();
}
```

- [ ] **Step 2: Confirmar que no compila**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~AuditoriaInterceptorTests"`
Expected: FAIL — `AuditoriaInterceptor` no existe.

- [ ] **Step 3: Implementar `AuditoriaInterceptor`**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RentaFacil.API.Models;

namespace RentaFacil.API.Data;

public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AplicarAuditoria(DbContext? context)
    {
        if (context == null) return;

        var usuarioId = ObtenerUsuarioIdActual();
        var ahora = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreadoPorId = usuarioId;
                entry.Entity.FechaCreacion = ahora;
                entry.Entity.ModificadoPorId = usuarioId;
                entry.Entity.FechaModificacion = ahora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModificadoPorId = usuarioId;
                entry.Entity.FechaModificacion = ahora;
            }
        }
    }

    private int? ObtenerUsuarioIdActual()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;
        var valor = usuario?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out var id) ? id : null;
    }
}
```

- [ ] **Step 4: Correr los tests**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~AuditoriaInterceptorTests"`
Expected: PASS (3 tests)

- [ ] **Step 5: Registrar el interceptor en `Program.cs`**

Reemplazar:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=rentafacil.db"));
```

Por:

```csharp
builder.Services.AddScoped<RentaFacil.API.Data.AuditoriaInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite("Data Source=rentafacil.db")
           .AddInterceptors(sp.GetRequiredService<RentaFacil.API.Data.AuditoriaInterceptor>()));
```

- [ ] **Step 6: Build y verificación manual**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

```bash
rm RentaFacil.API/rentafacil.db
dotnet run --project RentaFacil.API
```
Expected: arranca sin excepciones (el seed corre sin `HttpContext` activo, debe pasar por la rama "sin HttpContext" del interceptor sin romperse). Detener (Ctrl+C).

- [ ] **Step 7: Commit**

```bash
git add RentaFacil.API/Data/AuditoriaInterceptor.cs RentaFacil.API/Program.cs RentaFacil.Tests/AuditoriaInterceptorTests.cs
git commit -m "feat: add AuditoriaInterceptor for automatic change tracking"
```

---

### Task 12: IDOR en Inquilino

**Files:**
- Modify: `RentaFacil.API/Repositories/Interfaces/IInquilinoRepository.cs`
- Modify: `RentaFacil.API/Repositories/InquilinoRepository.cs`
- Modify: `RentaFacil.API/Services/Interfaces/IInquilinoService.cs`
- Modify: `RentaFacil.API/Services/InquilinoService.cs`
- Modify: `RentaFacil.API/Controllers/InquilinosController.cs`
- Modify: `RentaFacil.Shared/Models/InquilinoDto.cs`
- Modify: `RentaFacil.MAUI/Components/Pages/CrearInquilino.razor`
- Modify: `RentaFacil.Tests/InquilinoServiceTests.cs`

**Interfaces:**
- Consumes: `ClaimsPrincipalExtensions.ObtenerUsuarioId` (Task 8).
- Produces: `IInquilinoRepository.{GetAllAsync(int usuarioId), GetByIdAsync(int id, int usuarioId), AddAsync(Inquilino), UpdateAsync(Inquilino), DeleteAsync(int id, int usuarioId)}`; `IInquilinoService` con la misma forma (DTOs en vez de entidades, más `CrearAsync(CrearInquilinoDto, int usuarioId)`).

- [ ] **Step 1: Quitar `UsuarioId` del DTO de creación**

```csharp
namespace RentaFacil.Shared.Models;

public record CrearInquilinoDto(
    string NombreCompleto,
    string Identificacion,
    string? Telefono,
    string? FotoUrl
);

public record InquilinoDto(
    int Id,
    string NombreCompleto,
    string Identificacion,
    string? Telefono,
    string? FotoUrl,
    DateTime FechaRegistro,
    int UsuarioId
);
```

- [ ] **Step 2: Actualizar la interfaz del Repository**

```csharp
using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetAllAsync(int usuarioId);
    Task<Inquilino?> GetByIdAsync(int id, int usuarioId);
    Task<Inquilino> AddAsync(Inquilino inquilino);
    Task UpdateAsync(Inquilino inquilino);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 3: Actualizar la implementación del Repository**

```csharp
using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class InquilinoRepository : IInquilinoRepository
{
    private readonly AppDbContext _context;

    public InquilinoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Inquilino>> GetAllAsync(int usuarioId)
    {
        return await _context.Inquilinos.Where(i => i.UsuarioId == usuarioId).ToListAsync();
    }

    public async Task<Inquilino?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Inquilinos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    }

    public async Task<Inquilino> AddAsync(Inquilino inquilino)
    {
        _context.Inquilinos.Add(inquilino);
        await _context.SaveChangesAsync();
        return inquilino;
    }

    public async Task UpdateAsync(Inquilino inquilino)
    {
        _context.Inquilinos.Update(inquilino);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var inquilino = await _context.Inquilinos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
        if (inquilino != null)
        {
            _context.Inquilinos.Remove(inquilino);
            await _context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Actualizar la interfaz del Service**

```csharp
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInquilinoService
{
    Task<IEnumerable<InquilinoDto>> GetAllAsync(int usuarioId);
    Task<InquilinoDto?> GetByIdAsync(int id, int usuarioId);
    Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto, int usuarioId);
    Task UpdateAsync(int id, CrearInquilinoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 5: Actualizar la implementación del Service**

```csharp
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class InquilinoService : IInquilinoService
{
    private readonly IInquilinoRepository _repository;

    public InquilinoService(IInquilinoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<InquilinoDto>> GetAllAsync(int usuarioId)
    {
        var inquilinos = await _repository.GetAllAsync(usuarioId);
        return inquilinos.Select(MapToDto);
    }

    public async Task<InquilinoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var inquilino = await _repository.GetByIdAsync(id, usuarioId);
        return inquilino != null ? MapToDto(inquilino) : null;
    }

    public async Task<InquilinoDto> CrearAsync(CrearInquilinoDto dto, int usuarioId)
    {
        var inquilino = new Inquilino
        {
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            FotoUrl = dto.FotoUrl,
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(inquilino);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, CrearInquilinoDto dto, int usuarioId)
    {
        var inquilino = await _repository.GetByIdAsync(id, usuarioId);
        if (inquilino != null)
        {
            inquilino.NombreCompleto = dto.NombreCompleto;
            inquilino.Identificacion = dto.Identificacion;
            inquilino.Telefono = dto.Telefono;
            inquilino.FotoUrl = dto.FotoUrl;
            await _repository.UpdateAsync(inquilino);
        }
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static InquilinoDto MapToDto(Inquilino i)
    {
        return new InquilinoDto(i.Id, i.NombreCompleto, i.Identificacion, i.Telefono, i.FotoUrl, i.FechaRegistro, i.UsuarioId);
    }
}
```

- [ ] **Step 6: Actualizar el Controller**

```csharp
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InquilinosController : ControllerBase
{
    private readonly IInquilinoService _service;

    public InquilinosController(IInquilinoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inquilinos = await _service.GetAllAsync(User.ObtenerUsuarioId());
        return Ok(inquilinos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inquilino = await _service.GetByIdAsync(id, User.ObtenerUsuarioId());
        if (inquilino == null) return NotFound();
        return Ok(inquilino);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearInquilinoDto dto)
    {
        var result = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearInquilinoDto dto)
    {
        await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }
}
```

- [ ] **Step 7: Actualizar `CrearInquilino.razor` (quitar el `1` hardcodeado)**

En `RentaFacil.MAUI/Components/Pages/CrearInquilino.razor`, reemplazar:

```csharp
        var dto = new CrearInquilinoDto(nombreCompleto, identificacion, telefono, null, 1);
```

Por:

```csharp
        var dto = new CrearInquilinoDto(nombreCompleto, identificacion, telefono, null);
```

- [ ] **Step 8: Reescribir `InquilinoServiceTests.cs` (signatures nuevas + test de IDOR)**

```csharp
using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class InquilinoServiceTests
{
    private readonly Mock<IInquilinoRepository> _repositoryMock;
    private readonly InquilinoService _service;

    public InquilinoServiceTests()
    {
        _repositoryMock = new Mock<IInquilinoRepository>();
        _service = new InquilinoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnListOfInquilinos()
    {
        var inquilinos = new List<Inquilino>
        {
            new Inquilino { Id = 1, NombreCompleto = "Juan Perez", Identificacion = "123456", UsuarioId = 1 },
            new Inquilino { Id = 2, NombreCompleto = "Maria Gomez", Identificacion = "654321", UsuarioId = 1 }
        };

        _repositoryMock.Setup(repo => repo.GetAllAsync(1)).ReturnsAsync(inquilinos);

        var result = await _service.GetAllAsync(1);

        result.Should().HaveCount(2);
        result.First().NombreCompleto.Should().Be("Juan Perez");
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInquilinoDto()
    {
        var dto = new CrearInquilinoDto("Carlos Lopez", "789123", "555-1234", null);
        var entity = new Inquilino
        {
            Id = 3,
            NombreCompleto = dto.NombreCompleto,
            Identificacion = dto.Identificacion,
            Telefono = dto.Telefono,
            UsuarioId = 1
        };

        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Inquilino>())).ReturnsAsync(entity);

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
        result.Id.Should().Be(3);
        result.NombreCompleto.Should().Be("Carlos Lopez");
    }

    [Fact]
    public async Task GetByIdAsync_ConInquilinoDeOtroUsuario_DevuelveNull()
    {
        _repositoryMock.Setup(repo => repo.GetByIdAsync(5, 99)).ReturnsAsync((Inquilino?)null);

        var result = await _service.GetByIdAsync(5, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(repo => repo.GetByIdAsync(5, 99), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConInquilinoDeOtroUsuario_NoLlamaUpdate()
    {
        _repositoryMock.Setup(repo => repo.GetByIdAsync(5, 99)).ReturnsAsync((Inquilino?)null);
        var dto = new CrearInquilinoDto("Hackeado", "000", null, null);

        await _service.UpdateAsync(5, dto, 99);

        _repositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Inquilino>()), Times.Never);
    }
}
```

- [ ] **Step 9: Build y tests**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~InquilinoServiceTests"`
Expected: PASS (4 tests)

- [ ] **Step 10: Commit**

```bash
git add RentaFacil.API/Repositories/Interfaces/IInquilinoRepository.cs RentaFacil.API/Repositories/InquilinoRepository.cs RentaFacil.API/Services/Interfaces/IInquilinoService.cs RentaFacil.API/Services/InquilinoService.cs RentaFacil.API/Controllers/InquilinosController.cs RentaFacil.Shared/Models/InquilinoDto.cs RentaFacil.MAUI/Components/Pages/CrearInquilino.razor RentaFacil.Tests/InquilinoServiceTests.cs
git commit -m "fix: filter Inquilino by UsuarioId to close IDOR"
```

---

### Task 13: IDOR en Inmueble

**Files:**
- Modify: `RentaFacil.API/Repositories/Interfaces/IInmuebleRepository.cs`
- Modify: `RentaFacil.API/Repositories/InmuebleRepository.cs`
- Modify: `RentaFacil.API/Services/Interfaces/IInmuebleService.cs`
- Modify: `RentaFacil.API/Services/InmuebleService.cs`
- Modify: `RentaFacil.API/Controllers/InmueblesController.cs`
- Modify: `RentaFacil.Shared/Models/InmuebleDto.cs`
- Modify: `RentaFacil.MAUI/Components/Pages/CrearInmueble.razor`
- Modify: `RentaFacil.Tests/OtherServiceTests.cs`

**Interfaces:**
- Consumes: `ClaimsPrincipalExtensions.ObtenerUsuarioId` (Task 8).
- Produces: `IInmuebleRepository`/`IInmuebleService` con la misma forma que `IInquilinoRepository`/`IInquilinoService` (Task 12), aplicada a `Inmueble`. Las Tasks 14-16 (Unidad/Contrato/Pago) consumen `IInmuebleRepository.GetByIdAsync(id, usuarioId)` para validar pertenencia.

- [ ] **Step 1: Quitar `UsuarioId` de `CrearInmuebleDto`**

```csharp
using RentaFacil.Shared.Enums;

namespace RentaFacil.Shared.Models;

public record CrearInmuebleDto(
    string Nombre,
    string Direccion,
    TipoInmueble Tipo,
    decimal MontoRenta
);

public record InmuebleDto(
    int Id,
    string Nombre,
    string Direccion,
    TipoInmueble Tipo,
    decimal MontoRenta,
    int UsuarioId
);

public record CrearUnidadDto(
    string Nombre,
    decimal MontoRenta,
    int InmuebleId
);

public record UnidadDto(
    int Id,
    string Nombre,
    decimal MontoRenta,
    bool Ocupada,
    int InmuebleId
);
```

- [ ] **Step 2: Actualizar `IInmuebleRepository`**

```csharp
using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IInmuebleRepository
{
    Task<IEnumerable<Inmueble>> GetAllAsync(int usuarioId);
    Task<Inmueble?> GetByIdAsync(int id, int usuarioId);
    Task<Inmueble> AddAsync(Inmueble inmueble);
    Task UpdateAsync(Inmueble inmueble);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 3: Actualizar `InmuebleRepository`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class InmuebleRepository : IInmuebleRepository
{
    private readonly AppDbContext _context;

    public InmuebleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Inmueble>> GetAllAsync(int usuarioId)
    {
        return await _context.Inmuebles.Where(i => i.UsuarioId == usuarioId).ToListAsync();
    }

    public async Task<Inmueble?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Inmuebles.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    }

    public async Task<Inmueble> AddAsync(Inmueble inmueble)
    {
        _context.Inmuebles.Add(inmueble);
        await _context.SaveChangesAsync();
        return inmueble;
    }

    public async Task UpdateAsync(Inmueble inmueble)
    {
        _context.Inmuebles.Update(inmueble);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var inmueble = await _context.Inmuebles.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
        if (inmueble != null)
        {
            _context.Inmuebles.Remove(inmueble);
            await _context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Actualizar `IInmuebleService`**

```csharp
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IInmuebleService
{
    Task<IEnumerable<InmuebleDto>> GetAllAsync(int usuarioId);
    Task<InmuebleDto?> GetByIdAsync(int id, int usuarioId);
    Task<InmuebleDto> CrearAsync(CrearInmuebleDto dto, int usuarioId);
    Task UpdateAsync(int id, CrearInmuebleDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 5: Actualizar `InmuebleService`**

```csharp
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class InmuebleService : IInmuebleService
{
    private readonly IInmuebleRepository _repository;

    public InmuebleService(IInmuebleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<InmuebleDto>> GetAllAsync(int usuarioId)
    {
        var inmuebles = await _repository.GetAllAsync(usuarioId);
        return inmuebles.Select(MapToDto);
    }

    public async Task<InmuebleDto?> GetByIdAsync(int id, int usuarioId)
    {
        var inmueble = await _repository.GetByIdAsync(id, usuarioId);
        return inmueble != null ? MapToDto(inmueble) : null;
    }

    public async Task<InmuebleDto> CrearAsync(CrearInmuebleDto dto, int usuarioId)
    {
        var inmueble = new Inmueble
        {
            Nombre = dto.Nombre,
            Direccion = dto.Direccion,
            Tipo = dto.Tipo,
            MontoRenta = dto.MontoRenta,
            UsuarioId = usuarioId
        };

        var created = await _repository.AddAsync(inmueble);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, CrearInmuebleDto dto, int usuarioId)
    {
        var inmueble = await _repository.GetByIdAsync(id, usuarioId);
        if (inmueble != null)
        {
            inmueble.Nombre = dto.Nombre;
            inmueble.Direccion = dto.Direccion;
            inmueble.Tipo = dto.Tipo;
            inmueble.MontoRenta = dto.MontoRenta;
            await _repository.UpdateAsync(inmueble);
        }
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static InmuebleDto MapToDto(Inmueble i)
    {
        return new InmuebleDto(i.Id, i.Nombre, i.Direccion, i.Tipo, i.MontoRenta, i.UsuarioId);
    }
}
```

- [ ] **Step 6: Actualizar `InmueblesController`**

```csharp
using Microsoft.AspNetCore.Mvc;
using RentaFacil.API.Extensions;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InmueblesController : ControllerBase
{
    private readonly IInmuebleService _service;

    public InmueblesController(IInmuebleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inmuebles = await _service.GetAllAsync(User.ObtenerUsuarioId());
        return Ok(inmuebles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inmueble = await _service.GetByIdAsync(id, User.ObtenerUsuarioId());
        if (inmueble == null) return NotFound();
        return Ok(inmueble);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearInmuebleDto dto)
    {
        var result = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearInmuebleDto dto)
    {
        await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }
}
```

- [ ] **Step 7: Actualizar `CrearInmueble.razor` (quitar el `1` hardcodeado)**

En `RentaFacil.MAUI/Components/Pages/CrearInmueble.razor`, reemplazar:

```csharp
        var dto = new CrearInmuebleDto(nombre, direccion, tipo, montoRenta, 1);
```

Por:

```csharp
        var dto = new CrearInmuebleDto(nombre, direccion, tipo, montoRenta);
```

- [ ] **Step 8: Actualizar `InmuebleServiceTests` dentro de `OtherServiceTests.cs`**

En `RentaFacil.Tests/OtherServiceTests.cs`, reemplazar la clase `InmuebleServiceTests` completa por:

```csharp
public class InmuebleServiceTests
{
    private readonly Mock<IInmuebleRepository> _repositoryMock;
    private readonly InmuebleService _service;

    public InmuebleServiceTests()
    {
        _repositoryMock = new Mock<IInmuebleRepository>();
        _service = new InmuebleService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnInmuebles()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Inmueble> { new Inmueble { Id = 1, Nombre = "Casa 1", Direccion = "Calle 1", UsuarioId = 1 } });
        var result = await _service.GetAllAsync(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CrearAsync_ShouldReturnCreatedInmueble()
    {
        var dto = new CrearInmuebleDto("Edificio A", "Avenida 2", TipoInmueble.Multiple, 0);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Inmueble>())).ReturnsAsync(new Inmueble { Id = 2, Nombre = dto.Nombre, Tipo = dto.Tipo, UsuarioId = 1 });
        var result = await _service.CrearAsync(dto, 1);
        result.Nombre.Should().Be("Edificio A");
        result.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ConInmuebleDeOtroUsuario_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(3, 99)).ReturnsAsync((Inmueble?)null);
        var result = await _service.GetByIdAsync(3, 99);
        result.Should().BeNull();
    }
}
```

- [ ] **Step 9: Build y tests**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~InmuebleServiceTests"`
Expected: PASS (3 tests)

- [ ] **Step 10: Commit**

```bash
git add RentaFacil.API/Repositories/Interfaces/IInmuebleRepository.cs RentaFacil.API/Repositories/InmuebleRepository.cs RentaFacil.API/Services/Interfaces/IInmuebleService.cs RentaFacil.API/Services/InmuebleService.cs RentaFacil.API/Controllers/InmueblesController.cs RentaFacil.Shared/Models/InmuebleDto.cs RentaFacil.MAUI/Components/Pages/CrearInmueble.razor RentaFacil.Tests/OtherServiceTests.cs
git commit -m "fix: filter Inmueble by UsuarioId to close IDOR"
```

---

### Task 14: Unidad gana Repository/Service propio + `UsuarioId` + IDOR

**Files:**
- Modify: `RentaFacil.API/Models/Unidad.cs`
- Create: `RentaFacil.API/Repositories/Interfaces/IUnidadRepository.cs`
- Create: `RentaFacil.API/Repositories/UnidadRepository.cs`
- Create: `RentaFacil.API/Services/Interfaces/IUnidadService.cs`
- Create: `RentaFacil.API/Services/UnidadService.cs`
- Modify: `RentaFacil.API/Controllers/OtherControllers.cs`
- Modify: `RentaFacil.API/Program.cs`
- Create: migración EF Core `AddUsuarioIdToUnidad`
- Create: `RentaFacil.Tests/UnidadServiceTests.cs`

**Interfaces:**
- Consumes: `IInmuebleRepository.GetByIdAsync(int id, int usuarioId)` (Task 13), `ClaimsPrincipalExtensions.ObtenerUsuarioId` (Task 8).
- Produces: `IUnidadRepository.{GetAllAsync(int usuarioId), GetByIdAsync(int id, int usuarioId), AddAsync(Unidad), UpdateAsync(Unidad), DeleteAsync(int id, int usuarioId)}`; `IUnidadService.{GetAllAsync(int usuarioId), CrearAsync(CrearUnidadDto, int usuarioId): Task<UnidadDto?>, UpdateAsync(int id, CrearUnidadDto, int usuarioId): Task<bool>, DeleteAsync(int id, int usuarioId)}`. `UnidadesController` deja de usar `AppDbContext` directamente (cierra el hallazgo de `errores-conocidos.md`).

`UnidadService.CrearAsync` devuelve `null` si `dto.InmuebleId` no existe o no pertenece a `usuarioId` — esto es lo que cierra el IDOR de escritura (nadie puede crear una Unidad bajo un Inmueble ajeno).

- [ ] **Step 1: Agregar `UsuarioId` al modelo `Unidad`**

En `RentaFacil.API/Models/Unidad.cs`, agregar la propiedad después de `InmuebleId`:

```csharp
    public int UsuarioId { get; set; }
```

- [ ] **Step 2: Escribir el test que falla**

```csharp
using FluentAssertions;
using Moq;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services;
using RentaFacil.Shared.Models;

namespace RentaFacil.Tests;

public class UnidadServiceTests
{
    private readonly Mock<IUnidadRepository> _repositoryMock;
    private readonly Mock<IInmuebleRepository> _inmuebleRepositoryMock;
    private readonly UnidadService _service;

    public UnidadServiceTests()
    {
        _repositoryMock = new Mock<IUnidadRepository>();
        _inmuebleRepositoryMock = new Mock<IInmuebleRepository>();
        _service = new UnidadService(_repositoryMock.Object, _inmuebleRepositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_ConInmuebleDelUsuario_CreaLaUnidad()
    {
        var dto = new CrearUnidadDto("Depto 1", 300, 10);
        _inmuebleRepositoryMock.Setup(r => r.GetByIdAsync(10, 1)).ReturnsAsync(new Inmueble { Id = 10, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Unidad>())).ReturnsAsync(new Unidad { Id = 5, Nombre = "Depto 1", MontoRenta = 300, InmuebleId = 10, UsuarioId = 1 });

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Depto 1");
    }

    [Fact]
    public async Task CrearAsync_ConInmuebleDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearUnidadDto("Depto 1", 300, 10);
        _inmuebleRepositoryMock.Setup(r => r.GetByIdAsync(10, 99)).ReturnsAsync((Inmueble?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Unidad>()), Times.Never);
    }
}
```

- [ ] **Step 3: Confirmar que no compila**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~UnidadServiceTests"`
Expected: FAIL — `IUnidadRepository`/`UnidadService` no existen.

- [ ] **Step 4: Crear `IUnidadRepository`**

```csharp
using RentaFacil.API.Models;

namespace RentaFacil.API.Repositories.Interfaces;

public interface IUnidadRepository
{
    Task<IEnumerable<Unidad>> GetAllAsync(int usuarioId);
    Task<Unidad?> GetByIdAsync(int id, int usuarioId);
    Task<Unidad> AddAsync(Unidad unidad);
    Task UpdateAsync(Unidad unidad);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 5: Implementar `UnidadRepository`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentaFacil.API.Data;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Repositories;

public class UnidadRepository : IUnidadRepository
{
    private readonly AppDbContext _context;
    public UnidadRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Unidad>> GetAllAsync(int usuarioId) =>
        await _context.Unidades.Where(u => u.UsuarioId == usuarioId).ToListAsync();

    public async Task<Unidad?> GetByIdAsync(int id, int usuarioId) =>
        await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.UsuarioId == usuarioId);

    public async Task<Unidad> AddAsync(Unidad unidad)
    {
        _context.Unidades.Add(unidad);
        await _context.SaveChangesAsync();
        return unidad;
    }

    public async Task UpdateAsync(Unidad unidad)
    {
        _context.Unidades.Update(unidad);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        var unidad = await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.UsuarioId == usuarioId);
        if (unidad != null)
        {
            _context.Unidades.Remove(unidad);
            await _context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 6: Crear `IUnidadService`**

```csharp
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services.Interfaces;

public interface IUnidadService
{
    Task<IEnumerable<UnidadDto>> GetAllAsync(int usuarioId);
    Task<UnidadDto?> CrearAsync(CrearUnidadDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearUnidadDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 7: Implementar `UnidadService`**

```csharp
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;
using RentaFacil.API.Services.Interfaces;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Services;

public class UnidadService : IUnidadService
{
    private readonly IUnidadRepository _repository;
    private readonly IInmuebleRepository _inmuebleRepository;

    public UnidadService(IUnidadRepository repository, IInmuebleRepository inmuebleRepository)
    {
        _repository = repository;
        _inmuebleRepository = inmuebleRepository;
    }

    public async Task<IEnumerable<UnidadDto>> GetAllAsync(int usuarioId)
    {
        var unidades = await _repository.GetAllAsync(usuarioId);
        return unidades.Select(MapToDto);
    }

    public async Task<UnidadDto?> CrearAsync(CrearUnidadDto dto, int usuarioId)
    {
        var inmueble = await _inmuebleRepository.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return null;

        var unidad = new Unidad
        {
            Nombre = dto.Nombre,
            MontoRenta = dto.MontoRenta,
            InmuebleId = dto.InmuebleId,
            Ocupada = false,
            UsuarioId = usuarioId
        };
        var created = await _repository.AddAsync(unidad);
        return MapToDto(created);
    }

    public async Task<bool> UpdateAsync(int id, CrearUnidadDto dto, int usuarioId)
    {
        var unidad = await _repository.GetByIdAsync(id, usuarioId);
        if (unidad == null) return false;

        var inmueble = await _inmuebleRepository.GetByIdAsync(dto.InmuebleId, usuarioId);
        if (inmueble == null) return false;

        unidad.Nombre = dto.Nombre;
        unidad.MontoRenta = dto.MontoRenta;
        unidad.InmuebleId = dto.InmuebleId;
        await _repository.UpdateAsync(unidad);
        return true;
    }

    public async Task DeleteAsync(int id, int usuarioId)
    {
        await _repository.DeleteAsync(id, usuarioId);
    }

    private static UnidadDto MapToDto(Unidad u) => new(u.Id, u.Nombre, u.MontoRenta, u.Ocupada, u.InmuebleId);
}
```

- [ ] **Step 8: Correr los tests nuevos**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~UnidadServiceTests"`
Expected: PASS (2 tests)

- [ ] **Step 9: Refactorizar `UnidadesController` (quitar el acceso directo a `AppDbContext`)**

En `RentaFacil.API/Controllers/OtherControllers.cs`, reemplazar la clase `UnidadesController` completa por:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UnidadesController : ControllerBase
{
    private readonly IUnidadService _service;
    public UnidadesController(IUnidadService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync(User.ObtenerUsuarioId()));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearUnidadDto dto)
    {
        var resultado = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        if (resultado == null) return BadRequest(new { message = "El inmueble indicado no existe o no te pertenece." });
        return CreatedAtAction(nameof(GetAll), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrearUnidadDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return actualizado ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, User.ObtenerUsuarioId());
        return NoContent();
    }
}
```

Agregar el using `RentaFacil.API.Extensions` al inicio de `OtherControllers.cs` si no está. Quitar el using `Microsoft.EntityFrameworkCore` y `RentaFacil.API.Data` de ese archivo si ya no los usa ninguna otra clase del archivo (revisar `ContratosController`/`PagosController`, que no los necesitan).

- [ ] **Step 10: Generar la migración**

```bash
dotnet ef migrations add AddUsuarioIdToUnidad --project RentaFacil.API --startup-project RentaFacil.API
```

- [ ] **Step 11: Registrar `IUnidadRepository`/`IUnidadService` en `Program.cs`**

Después de la línea que registra `IReciboService`, agregar:

```csharp
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IUnidadRepository, RentaFacil.API.Repositories.UnidadRepository>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IUnidadService, RentaFacil.API.Services.UnidadService>();
```

- [ ] **Step 12: Extender el seed para remapear `Unidad.UsuarioId`**

En el bloque de seed de `Program.cs` (Task 9), después de la línea `foreach (var inmueble in context.Inmuebles) inmueble.UsuarioId = admin.Id;` y antes de `context.SaveChanges();`, agregar el remapeo de Unidad en su propia ronda de `SaveChanges` (porque depende de que `Inmueble.UsuarioId` ya esté guardado):

```csharp
        foreach (var inquilino in context.Inquilinos) inquilino.UsuarioId = admin.Id;
        foreach (var inmueble in context.Inmuebles) inmueble.UsuarioId = admin.Id;
        context.SaveChanges();

        foreach (var unidad in context.Unidades.Include(u => u.Inmueble))
        {
            unidad.UsuarioId = unidad.Inmueble.UsuarioId;
        }
        context.SaveChanges();
```

Y en el seed de datos dummy (dentro del `if (!context.Inquilinos.Any())`), actualizar la creación de `Unidad` para incluir `UsuarioId`:

```csharp
            var uni = new RentaFacil.API.Models.Unidad { Nombre = "Apt 1A", MontoRenta = 500, Ocupada = true, InmuebleId = inm.Id, UsuarioId = admin.Id };
```

- [ ] **Step 13: Build y verificación manual**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

```bash
rm RentaFacil.API/rentafacil.db
dotnet run --project RentaFacil.API
```
Expected: arranca sin errores, la Unidad dummy queda con `UsuarioId` del admin. Detener (Ctrl+C).

- [ ] **Step 14: Commit**

```bash
git add RentaFacil.API/Models/Unidad.cs RentaFacil.API/Repositories/Interfaces/IUnidadRepository.cs RentaFacil.API/Repositories/UnidadRepository.cs RentaFacil.API/Services/Interfaces/IUnidadService.cs RentaFacil.API/Services/UnidadService.cs RentaFacil.API/Controllers/OtherControllers.cs RentaFacil.API/Program.cs RentaFacil.API/Migrations RentaFacil.Tests/UnidadServiceTests.cs
git commit -m "refactor: give Unidad its own Repository/Service, filter by UsuarioId"
```

---

### Task 15: IDOR en Contrato (con validación de pertenencia de Inquilino y Unidad)

**Files:**
- Modify: `RentaFacil.API/Models/Contrato.cs`
- Modify: `RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs`
- Modify: `RentaFacil.API/Repositories/OtherRepositories.cs`
- Modify: `RentaFacil.API/Services/Interfaces/IOtherServices.cs`
- Modify: `RentaFacil.API/Services/OtherServices.cs`
- Modify: `RentaFacil.API/Controllers/OtherControllers.cs`
- Modify: `RentaFacil.API/Program.cs`
- Create: migración EF Core `AddUsuarioIdToContrato`
- Modify: `RentaFacil.Tests/OtherServiceTests.cs`

**Interfaces:**
- Consumes: `IInquilinoRepository.GetByIdAsync(int id, int usuarioId)` (Task 12), `IUnidadRepository.GetByIdAsync(int id, int usuarioId)` (Task 14).
- Produces: `IContratoRepository`/`IContratoService` con la forma usuarioId-filtrada; `ContratoService.CrearAsync` devuelve `null` si `InquilinoId` o `UnidadId` no pertenecen a `usuarioId`.

- [ ] **Step 1: Agregar `UsuarioId` al modelo `Contrato`**

En `RentaFacil.API/Models/Contrato.cs`, agregar después de `Activo`:

```csharp
    public int UsuarioId { get; set; }
```

- [ ] **Step 2: Actualizar `IContratoRepository`**

En `RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs`, reemplazar la interfaz `IContratoRepository` por:

```csharp
public interface IContratoRepository
{
    Task<IEnumerable<Contrato>> GetAllAsync(int usuarioId);
    Task<Contrato?> GetByIdAsync(int id, int usuarioId);
    Task<Contrato> AddAsync(Contrato contrato);
    Task UpdateAsync(Contrato contrato);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 3: Actualizar `ContratoRepository`**

En `RentaFacil.API/Repositories/OtherRepositories.cs`, reemplazar la clase `ContratoRepository` por:

```csharp
public class ContratoRepository : IContratoRepository
{
    private readonly AppDbContext _context;
    public ContratoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Contrato>> GetAllAsync(int usuarioId) =>
        await _context.Contratos.Where(c => c.UsuarioId == usuarioId).ToListAsync();
    public async Task<Contrato?> GetByIdAsync(int id, int usuarioId) =>
        await _context.Contratos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    public async Task<Contrato> AddAsync(Contrato contrato)
    {
        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();
        return contrato;
    }
    public async Task UpdateAsync(Contrato contrato)
    {
        _context.Contratos.Update(contrato);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id, int usuarioId)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (contrato != null)
        {
            _context.Contratos.Remove(contrato);
            await _context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Actualizar `IContratoService`**

En `RentaFacil.API/Services/Interfaces/IOtherServices.cs`, reemplazar la interfaz `IContratoService` por:

```csharp
public interface IContratoService
{
    Task<IEnumerable<ContratoDto>> GetAllAsync(int usuarioId);
    Task<ContratoDto?> GetByIdAsync(int id, int usuarioId);
    Task<ContratoDto?> CrearAsync(CrearContratoDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearContratoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 5: Actualizar `ContratoService`**

En `RentaFacil.API/Services/OtherServices.cs`, reemplazar la clase `ContratoService` por:

```csharp
public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repository;
    private readonly IInquilinoRepository _inquilinoRepository;
    private readonly IUnidadRepository _unidadRepository;

    public ContratoService(IContratoRepository repository, IInquilinoRepository inquilinoRepository, IUnidadRepository unidadRepository)
    {
        _repository = repository;
        _inquilinoRepository = inquilinoRepository;
        _unidadRepository = unidadRepository;
    }

    public async Task<IEnumerable<ContratoDto>> GetAllAsync(int usuarioId)
    {
        var contratos = await _repository.GetAllAsync(usuarioId);
        return contratos.Select(MapToDto);
    }
    public async Task<ContratoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var contrato = await _repository.GetByIdAsync(id, usuarioId);
        return contrato != null ? MapToDto(contrato) : null;
    }
    public async Task<ContratoDto?> CrearAsync(CrearContratoDto dto, int usuarioId)
    {
        var inquilino = await _inquilinoRepository.GetByIdAsync(dto.InquilinoId, usuarioId);
        if (inquilino == null) return null;
        var unidad = await _unidadRepository.GetByIdAsync(dto.UnidadId, usuarioId);
        if (unidad == null) return null;

        var contrato = new Contrato
        {
            InquilinoId = dto.InquilinoId, UnidadId = dto.UnidadId,
            Monto = dto.Monto, Garantia = dto.Garantia,
            DuracionMeses = dto.DuracionMeses, DiaPago = dto.DiaPago,
            FechaInicio = dto.FechaInicio, FechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses),
            Observaciones = dto.Observaciones, Activo = true, UsuarioId = usuarioId
        };
        var created = await _repository.AddAsync(contrato);
        return MapToDto(created);
    }
    public async Task<bool> UpdateAsync(int id, CrearContratoDto dto, int usuarioId)
    {
        var contrato = await _repository.GetByIdAsync(id, usuarioId);
        if (contrato == null) return false;

        var inquilino = await _inquilinoRepository.GetByIdAsync(dto.InquilinoId, usuarioId);
        if (inquilino == null) return false;
        var unidad = await _unidadRepository.GetByIdAsync(dto.UnidadId, usuarioId);
        if (unidad == null) return false;

        contrato.InquilinoId = dto.InquilinoId; contrato.UnidadId = dto.UnidadId;
        contrato.Monto = dto.Monto; contrato.Garantia = dto.Garantia;
        contrato.DuracionMeses = dto.DuracionMeses; contrato.DiaPago = dto.DiaPago;
        contrato.FechaInicio = dto.FechaInicio; contrato.FechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses);
        contrato.Observaciones = dto.Observaciones;
        await _repository.UpdateAsync(contrato);
        return true;
    }
    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private static ContratoDto MapToDto(Contrato c) => new(c.Id, c.InquilinoId, c.UnidadId, c.Monto, c.Garantia, c.DuracionMeses, c.DiaPago, c.FechaInicio, c.FechaFin, c.Observaciones, c.Activo);
}
```

- [ ] **Step 6: Actualizar `ContratosController`**

En `RentaFacil.API/Controllers/OtherControllers.cs`, reemplazar la clase `ContratosController` por:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ContratosController : ControllerBase
{
    private readonly IContratoService _service;
    public ContratosController(IContratoService service) => _service = service;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync(User.ObtenerUsuarioId()));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var res = await _service.GetByIdAsync(id, User.ObtenerUsuarioId()); return res == null ? NotFound() : Ok(res); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CrearContratoDto dto)
    {
        var res = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        if (res == null) return BadRequest(new { message = "El inquilino o la unidad indicados no existen o no te pertenecen." });
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CrearContratoDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return actualizado ? NoContent() : NotFound();
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id, User.ObtenerUsuarioId()); return NoContent(); }
}
```

- [ ] **Step 7: Generar la migración**

```bash
dotnet ef migrations add AddUsuarioIdToContrato --project RentaFacil.API --startup-project RentaFacil.API
```

- [ ] **Step 8: Extender el seed para remapear `Contrato.UsuarioId`**

En el bloque de seed (después del remapeo de `Unidad` agregado en la Task 14), agregar:

```csharp
        foreach (var contrato in context.Contratos.Include(c => c.Inquilino))
        {
            contrato.UsuarioId = contrato.Inquilino.UsuarioId;
        }
        context.SaveChanges();
```

Y en el seed de datos dummy, actualizar la creación de `Contrato`:

```csharp
            var con = new RentaFacil.API.Models.Contrato { InquilinoId = inq.Id, UnidadId = uni.Id, Monto = 500, Garantia = 500, Frecuencia = RentaFacil.Shared.Enums.FrecuenciaPago.Mensual, DuracionMeses = 12, DiaPago = 5, FechaInicio = DateTime.Now, FechaFin = DateTime.Now.AddMonths(12), Activo = true, UsuarioId = admin.Id };
```

- [ ] **Step 9: Reescribir `ContratoServiceTests` dentro de `OtherServiceTests.cs`**

Reemplazar la clase `ContratoServiceTests` por:

```csharp
public class ContratoServiceTests
{
    private readonly Mock<IContratoRepository> _repositoryMock;
    private readonly Mock<IInquilinoRepository> _inquilinoRepositoryMock;
    private readonly Mock<IUnidadRepository> _unidadRepositoryMock;
    private readonly ContratoService _service;

    public ContratoServiceTests()
    {
        _repositoryMock = new Mock<IContratoRepository>();
        _inquilinoRepositoryMock = new Mock<IInquilinoRepository>();
        _unidadRepositoryMock = new Mock<IUnidadRepository>();
        _service = new ContratoService(_repositoryMock.Object, _inquilinoRepositoryMock.Object, _unidadRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnContratos()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<Contrato> { new Contrato { Id = 1, Monto = 500, UsuarioId = 1 } });
        var result = await _service.GetAllAsync(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CrearAsync_ConInquilinoYUnidadDelUsuario_CreaElContrato()
    {
        var dto = new CrearContratoDto(1, 2, 500, 500, 12, 5, DateTime.Now, null);
        _inquilinoRepositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new Inquilino { Id = 1, UsuarioId = 1 });
        _unidadRepositoryMock.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(new Unidad { Id = 2, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contrato>())).ReturnsAsync(new Contrato { Id = 3, InquilinoId = 1, UnidadId = 2, Monto = 500, UsuarioId = 1 });

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CrearAsync_ConUnidadDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearContratoDto(1, 2, 500, 500, 12, 5, DateTime.Now, null);
        _inquilinoRepositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync(new Inquilino { Id = 1, UsuarioId = 99 });
        _unidadRepositoryMock.Setup(r => r.GetByIdAsync(2, 99)).ReturnsAsync((Unidad?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contrato>()), Times.Never);
    }
}
```

- [ ] **Step 10: Build y tests**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~ContratoServiceTests"`
Expected: PASS (3 tests)

- [ ] **Step 11: Commit**

```bash
git add RentaFacil.API/Models/Contrato.cs RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs RentaFacil.API/Repositories/OtherRepositories.cs RentaFacil.API/Services/Interfaces/IOtherServices.cs RentaFacil.API/Services/OtherServices.cs RentaFacil.API/Controllers/OtherControllers.cs RentaFacil.API/Program.cs RentaFacil.API/Migrations RentaFacil.Tests/OtherServiceTests.cs
git commit -m "fix: filter Contrato by UsuarioId and validate Inquilino/Unidad ownership"
```

---

### Task 16: IDOR en Pago (con validación de pertenencia de Contrato) + recibo PDF

**Files:**
- Modify: `RentaFacil.API/Models/Pago.cs`
- Modify: `RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs`
- Modify: `RentaFacil.API/Repositories/OtherRepositories.cs`
- Modify: `RentaFacil.API/Services/Interfaces/IOtherServices.cs`
- Modify: `RentaFacil.API/Services/OtherServices.cs`
- Modify: `RentaFacil.API/Services/ReciboService.cs`
- Modify: `RentaFacil.API/Controllers/OtherControllers.cs`
- Modify: `RentaFacil.API/Program.cs`
- Create: migración EF Core `AddUsuarioIdToPago`
- Modify: `RentaFacil.Tests/OtherServiceTests.cs`

**Interfaces:**
- Consumes: `IContratoRepository.GetByIdAsync(int id, int usuarioId)` (Task 15).
- Produces: `IPagoRepository`/`IPagoService` usuarioId-filtrados; `IReciboService.GenerarReciboPdfAsync(int pagoId, string formato, int usuarioId)` — ya no se puede generar el recibo de un pago ajeno.

- [ ] **Step 1: Agregar `UsuarioId` al modelo `Pago`**

En `RentaFacil.API/Models/Pago.cs`, agregar después de `Completado`:

```csharp
    public int UsuarioId { get; set; }
```

- [ ] **Step 2: Actualizar `IPagoRepository`**

En `RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs`, reemplazar `IPagoRepository` por:

```csharp
public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetAllAsync(int usuarioId);
    Task<Pago?> GetByIdAsync(int id, int usuarioId);
    Task<Pago> AddAsync(Pago pago);
    Task UpdateAsync(Pago pago);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 3: Actualizar `PagoRepository`**

En `RentaFacil.API/Repositories/OtherRepositories.cs`, reemplazar la clase `PagoRepository` por:

```csharp
public class PagoRepository : IPagoRepository
{
    private readonly AppDbContext _context;
    public PagoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Pago>> GetAllAsync(int usuarioId) =>
        await _context.Pagos.Where(p => p.UsuarioId == usuarioId).ToListAsync();
    public async Task<Pago?> GetByIdAsync(int id, int usuarioId) =>
        await _context.Pagos.FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);
    public async Task<Pago> AddAsync(Pago pago)
    {
        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();
        return pago;
    }
    public async Task UpdateAsync(Pago pago)
    {
        _context.Pagos.Update(pago);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id, int usuarioId)
    {
        var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
        if (pago != null)
        {
            _context.Pagos.Remove(pago);
            await _context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Actualizar `IPagoService`**

En `RentaFacil.API/Services/Interfaces/IOtherServices.cs`, reemplazar `IPagoService` por:

```csharp
public interface IPagoService
{
    Task<IEnumerable<PagoDto>> GetAllAsync(int usuarioId);
    Task<PagoDto?> GetByIdAsync(int id, int usuarioId);
    Task<PagoDto?> CrearAsync(CrearPagoDto dto, int usuarioId);
    Task<bool> UpdateAsync(int id, CrearPagoDto dto, int usuarioId);
    Task DeleteAsync(int id, int usuarioId);
}
```

- [ ] **Step 5: Actualizar `PagoService`**

En `RentaFacil.API/Services/OtherServices.cs`, reemplazar la clase `PagoService` por:

```csharp
public class PagoService : IPagoService
{
    private readonly IPagoRepository _repository;
    private readonly IContratoRepository _contratoRepository;

    public PagoService(IPagoRepository repository, IContratoRepository contratoRepository)
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
    }

    public async Task<IEnumerable<PagoDto>> GetAllAsync(int usuarioId)
    {
        var pagos = await _repository.GetAllAsync(usuarioId);
        return pagos.Select(MapToDto);
    }
    public async Task<PagoDto?> GetByIdAsync(int id, int usuarioId)
    {
        var pago = await _repository.GetByIdAsync(id, usuarioId);
        return pago != null ? MapToDto(pago) : null;
    }
    public async Task<PagoDto?> CrearAsync(CrearPagoDto dto, int usuarioId)
    {
        var contrato = await _contratoRepository.GetByIdAsync(dto.ContratoId, usuarioId);
        if (contrato == null) return null;

        var pago = new Pago
        {
            ContratoId = dto.ContratoId, TotalMonto = dto.TotalMonto,
            ACuenta = dto.ACuenta, Servicios = dto.Servicios,
            FechaPago = dto.FechaPago, Periodo = dto.Periodo,
            Facturado = false, Completado = dto.ACuenta >= dto.TotalMonto,
            UsuarioId = usuarioId
        };
        var created = await _repository.AddAsync(pago);
        return MapToDto(created);
    }
    public async Task<bool> UpdateAsync(int id, CrearPagoDto dto, int usuarioId)
    {
        var pago = await _repository.GetByIdAsync(id, usuarioId);
        if (pago == null) return false;

        var contrato = await _contratoRepository.GetByIdAsync(dto.ContratoId, usuarioId);
        if (contrato == null) return false;

        pago.ContratoId = dto.ContratoId; pago.TotalMonto = dto.TotalMonto;
        pago.ACuenta = dto.ACuenta; pago.Servicios = dto.Servicios;
        pago.FechaPago = dto.FechaPago; pago.Periodo = dto.Periodo;
        pago.Completado = dto.ACuenta >= dto.TotalMonto;
        await _repository.UpdateAsync(pago);
        return true;
    }
    public async Task DeleteAsync(int id, int usuarioId) => await _repository.DeleteAsync(id, usuarioId);

    private static PagoDto MapToDto(Pago p) => new(p.Id, p.ContratoId, p.TotalMonto, p.ACuenta, p.Servicios, p.FechaPago, p.Periodo, p.Facturado, p.Completado);
}
```

- [ ] **Step 6: Actualizar `IReciboService`/`ReciboService`**

En `RentaFacil.API/Services/ReciboService.cs`, cambiar la firma de la interfaz:

```csharp
public interface IReciboService
{
    Task<byte[]> GenerarReciboPdfAsync(int pagoId, string formato, int usuarioId);
}
```

Y el método `GenerarReciboPdfAsync` completo (solo cambian la firma y las tres líneas que ahora pasan `usuarioId`; el resto —generación del PDF con QuestPDF— es exactamente el código ya existente en el archivo):

```csharp
        public async Task<byte[]> GenerarReciboPdfAsync(int pagoId, string formato, int usuarioId)
        {
            var pago = await _pagoRepository.GetByIdAsync(pagoId, usuarioId);
            if (pago == null) throw new Exception("Pago no encontrado");

            var contrato = await _contratoRepository.GetByIdAsync(pago.ContratoId, usuarioId);
            if (contrato == null) throw new Exception("Contrato no encontrado");

            var inquilino = await _inquilinoRepository.GetByIdAsync(contrato.InquilinoId, usuarioId);
            if (inquilino == null) throw new Exception("Inquilino no encontrado");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    if (formato.ToLower() == "ticket")
                    {
                        page.Size(80, 200, Unit.Millimetre); // Ticket 80mm ancho, largo dinámico
                        page.Margin(5, Unit.Millimetre);
                    }
                    else
                    {
                        page.Size(PageSizes.A4); // Carta/A4
                        page.Margin(1, Unit.Centimetre);
                    }

                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(formato.ToLower() == "ticket" ? 10 : 12));

                    page.Header().Element(compose => ComposeHeader(compose, pago, inquilino, formato));
                    page.Content().Element(compose => ComposeContent(compose, pago, formato));

                    if (formato.ToLower() != "ticket")
                    {
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Generado por RentaFácilApp - Página ");
                            x.CurrentPageNumber();
                        });
                    }
                });
            });

            return document.GeneratePdf();
        }
```

No es necesario tocar `ComposeHeader`/`ComposeContent` — siguen igual que en el archivo actual.

- [ ] **Step 7: Actualizar `PagosController`**

En `RentaFacil.API/Controllers/OtherControllers.cs`, reemplazar la clase `PagosController` por:

```csharp
[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _service;
    private readonly IReciboService _reciboService;

    public PagosController(IPagoService service, IReciboService reciboService)
    {
        _service = service;
        _reciboService = reciboService;
    }

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync(User.ObtenerUsuarioId()));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var res = await _service.GetByIdAsync(id, User.ObtenerUsuarioId()); return res == null ? NotFound() : Ok(res); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CrearPagoDto dto)
    {
        var res = await _service.CrearAsync(dto, User.ObtenerUsuarioId());
        if (res == null) return BadRequest(new { message = "El contrato indicado no existe o no te pertenece." });
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CrearPagoDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto, User.ObtenerUsuarioId());
        return actualizado ? NoContent() : NotFound();
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id, User.ObtenerUsuarioId()); return NoContent(); }

    [HttpGet("{id}/recibo/{formato}")]
    public async Task<IActionResult> GetRecibo(int id, string formato)
    {
        try
        {
            var pdfBytes = await _reciboService.GenerarReciboPdfAsync(id, formato, User.ObtenerUsuarioId());
            return File(pdfBytes, "application/pdf", $"Recibo_Pago_{id}.pdf");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
```

- [ ] **Step 8: Generar la migración**

```bash
dotnet ef migrations add AddUsuarioIdToPago --project RentaFacil.API --startup-project RentaFacil.API
```

- [ ] **Step 9: Extender el seed para remapear `Pago.UsuarioId`**

En el bloque de seed, después del remapeo de `Contrato` (Task 15), agregar:

```csharp
        foreach (var pago in context.Pagos.Include(p => p.Contrato))
        {
            pago.UsuarioId = pago.Contrato.UsuarioId;
        }
        context.SaveChanges();
```

Y en el seed de datos dummy, actualizar la creación de `Pago`:

```csharp
            var pag = new RentaFacil.API.Models.Pago { ContratoId = con.Id, TotalMonto = 500, ACuenta = 200, Servicios = 0, FechaPago = DateTime.Now, Periodo = "MAY-26", Facturado = false, Completado = false, UsuarioId = admin.Id };
```

- [ ] **Step 10: Reescribir `PagoServiceTests` dentro de `OtherServiceTests.cs`**

Reemplazar la clase `PagoServiceTests` por:

```csharp
public class PagoServiceTests
{
    private readonly Mock<IPagoRepository> _repositoryMock;
    private readonly Mock<IContratoRepository> _contratoRepositoryMock;
    private readonly PagoService _service;

    public PagoServiceTests()
    {
        _repositoryMock = new Mock<IPagoRepository>();
        _contratoRepositoryMock = new Mock<IContratoRepository>();
        _service = new PagoService(_repositoryMock.Object, _contratoRepositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_ShouldCalculateCompletado()
    {
        var dto = new CrearPagoDto(1, 500, 500, 0, DateTime.Now, "MAY-26");
        _contratoRepositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new Contrato { Id = 1, UsuarioId = 1 });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Pago>())).ReturnsAsync(new Pago { Id = 1, TotalMonto = 500, ACuenta = 500, Completado = true, Periodo = "MAY-26", UsuarioId = 1 });

        var result = await _service.CrearAsync(dto, 1);

        result.Should().NotBeNull();
        result!.Completado.Should().BeTrue();
    }

    [Fact]
    public async Task CrearAsync_ConContratoDeOtroUsuario_DevuelveNull()
    {
        var dto = new CrearPagoDto(1, 500, 500, 0, DateTime.Now, "MAY-26");
        _contratoRepositoryMock.Setup(r => r.GetByIdAsync(1, 99)).ReturnsAsync((Contrato?)null);

        var result = await _service.CrearAsync(dto, 99);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Pago>()), Times.Never);
    }
}
```

- [ ] **Step 11: Build y tests completos del proyecto**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

Run: `dotnet test RentaFacil.Tests`
Expected: todos los tests existentes PASS (incluye Inquilino, Inmueble, Unidad, Contrato, Pago, Auth, Claims, Auditoría).

- [ ] **Step 12: Verificación manual end-to-end del cierre de IDOR**

```bash
rm RentaFacil.API/rentafacil.db
dotnet run --project RentaFacil.API
```

En otra terminal: registrar un segundo usuario con el token del admin, hacer login con ambos, y confirmar que el segundo usuario NO ve los datos dummy del primero.

```bash
TOKEN_ADMIN=$(curl -s -X POST http://localhost:5295/api/auth/login -H "Content-Type: application/json" -d '{"nombreUsuario":"duenotest","password":"CambiaEstaClave123!"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")
curl -s -X POST http://localhost:5295/api/auth/registrar -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN_ADMIN" -d '{"nombreUsuario":"otro","password":"OtraClave123!","rol":"Propietario"}'
TOKEN_OTRO=$(curl -s -X POST http://localhost:5295/api/auth/login -H "Content-Type: application/json" -d '{"nombreUsuario":"otro","password":"OtraClave123!"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")
curl -s -H "Authorization: Bearer $TOKEN_OTRO" http://localhost:5295/api/inquilinos
```
Expected: `[]` (lista vacía) — el usuario `otro` no ve al inquilino dummy del admin. Detener la API (Ctrl+C).

- [ ] **Step 13: Commit**

```bash
git add RentaFacil.API/Models/Pago.cs RentaFacil.API/Repositories/Interfaces/IOtherRepositories.cs RentaFacil.API/Repositories/OtherRepositories.cs RentaFacil.API/Services/Interfaces/IOtherServices.cs RentaFacil.API/Services/OtherServices.cs RentaFacil.API/Services/ReciboService.cs RentaFacil.API/Controllers/OtherControllers.cs RentaFacil.API/Program.cs RentaFacil.API/Migrations RentaFacil.Tests/OtherServiceTests.cs
git commit -m "fix: filter Pago by UsuarioId, validate Contrato ownership, secure recibo PDF"
```

---

### Task 17: Validación de cédula/RUC ecuatoriano

**Files:**
- Create: `RentaFacil.Shared/Validaciones/IdentificacionEcuatorianaAttribute.cs`
- Modify: `RentaFacil.Shared/Models/InquilinoDto.cs`
- Create: `RentaFacil.Tests/IdentificacionEcuatorianaAttributeTests.cs`

**Interfaces:**
- Produces: `IdentificacionEcuatorianaAttribute : ValidationAttribute`, aplicado a `CrearInquilinoDto.Identificacion`. Acepta cédula de 10 dígitos (persona natural, tercer dígito 0-5) y RUC de 13 dígitos (persona natural 0-5, o sociedad privada con tercer dígito 9). Rechaza cualquier otro formato (incluye RUC de sector público, tercer dígito 6-8).

- [ ] **Step 1: Escribir los tests que fallan**

Los siguientes vectores fueron derivados a mano con el algoritmo módulo 10 (cédula/RUC natural) y módulo 11 (RUC sociedad) — no corresponden a personas/empresas reales:
- Cédula válida: `1712345675` (provincia 17, tercer dígito 1, dígito verificador 5).
- RUC natural válido: `1712345675001` (la cédula anterior + sufijo `001`).
- RUC sociedad válido: `1791234561001` (base `179123456`, dígito verificador 1, sufijo `001`).

```csharp
using FluentAssertions;
using RentaFacil.Shared.Validaciones;

namespace RentaFacil.Tests;

public class IdentificacionEcuatorianaAttributeTests
{
    private readonly IdentificacionEcuatorianaAttribute _attribute = new();

    [Theory]
    [InlineData("1712345675")]      // cédula válida
    [InlineData("1712345675001")]   // RUC persona natural válido
    [InlineData("1791234561001")]   // RUC sociedad válido
    public void IsValid_ConIdentificacionValida_DevuelveTrue(string identificacion)
    {
        _attribute.IsValid(identificacion).Should().BeTrue();
    }

    [Theory]
    [InlineData("1712345674")]      // cédula con dígito verificador incorrecto
    [InlineData("0012345675")]      // provincia inválida (00)
    [InlineData("1762345675")]      // tercer dígito inválido para cédula (6)
    [InlineData("1712345675000")]   // RUC natural con sufijo 000
    [InlineData("1791234562001")]   // RUC sociedad con dígito verificador incorrecto
    [InlineData("1771234567001")]   // RUC con tercer dígito no soportado (7, sector público/otros)
    [InlineData("171234567A")]      // contiene una letra
    [InlineData("12345")]           // longitud inválida
    [InlineData("")]                // vacío
    public void IsValid_ConIdentificacionInvalida_DevuelveFalse(string identificacion)
    {
        _attribute.IsValid(identificacion).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Confirmar que no compila**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~IdentificacionEcuatorianaAttributeTests"`
Expected: FAIL — `IdentificacionEcuatorianaAttribute` no existe.

- [ ] **Step 3: Implementar el atributo**

```csharp
using System.ComponentModel.DataAnnotations;

namespace RentaFacil.Shared.Validaciones;

public class IdentificacionEcuatorianaAttribute : ValidationAttribute
{
    public IdentificacionEcuatorianaAttribute()
    {
        ErrorMessage = "La identificación no es una cédula o RUC ecuatoriano válido.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string identificacion || string.IsNullOrWhiteSpace(identificacion))
        {
            return false;
        }

        if (!identificacion.All(char.IsDigit))
        {
            return false;
        }

        return identificacion.Length switch
        {
            10 => EsCedulaValida(identificacion),
            13 => EsRucValido(identificacion),
            _ => false
        };
    }

    private static bool EsCedulaValida(string cedula)
    {
        var provincia = int.Parse(cedula[..2]);
        var tercerDigito = cedula[2] - '0';
        if (provincia < 1 || provincia > 24) return false;
        if (tercerDigito is < 0 or > 5) return false;

        return ValidarModulo10(cedula[..9]) == cedula[9] - '0';
    }

    private static bool EsRucValido(string ruc)
    {
        var provincia = int.Parse(ruc[..2]);
        if (provincia < 1 || provincia > 24) return false;

        var tercerDigito = ruc[2] - '0';
        var sufijo = ruc[10..];

        if (tercerDigito is >= 0 and <= 5)
        {
            if (sufijo == "000") return false;
            return ValidarModulo10(ruc[..9]) == ruc[9] - '0';
        }

        if (tercerDigito == 9)
        {
            if (sufijo == "000") return false;
            return ValidarModulo11Sociedad(ruc[..9]) == ruc[9] - '0';
        }

        return false;
    }

    private static int ValidarModulo10(string nueveDigitos)
    {
        int[] coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            var producto = (nueveDigitos[i] - '0') * coeficientes[i];
            suma += producto >= 10 ? producto - 9 : producto;
        }
        var residuo = suma % 10;
        return residuo == 0 ? 0 : 10 - residuo;
    }

    private static int? ValidarModulo11Sociedad(string nueveDigitos)
    {
        int[] coeficientes = [4, 3, 2, 7, 6, 5, 4, 3, 2];
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            suma += (nueveDigitos[i] - '0') * coeficientes[i];
        }
        var residuo = suma % 11;
        var digitoVerificador = residuo == 0 ? 0 : 11 - residuo;
        return digitoVerificador == 11 ? null : digitoVerificador;
    }
}
```

- [ ] **Step 4: Correr los tests**

Run: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~IdentificacionEcuatorianaAttributeTests"`
Expected: PASS (12 casos: 3 válidos + 9 inválidos)

- [ ] **Step 5: Aplicar el atributo a `CrearInquilinoDto.Identificacion`**

```csharp
using RentaFacil.Shared.Validaciones;

namespace RentaFacil.Shared.Models;

public record CrearInquilinoDto(
    string NombreCompleto,
    [property: IdentificacionEcuatoriana] string Identificacion,
    string? Telefono,
    string? FotoUrl
);

public record InquilinoDto(
    int Id,
    string NombreCompleto,
    string Identificacion,
    string? Telefono,
    string? FotoUrl,
    DateTime FechaRegistro,
    int UsuarioId
);
```

- [ ] **Step 6: Build completo**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.`

- [ ] **Step 7: Verificación manual — la API rechaza una identificación inválida**

```bash
dotnet run --project RentaFacil.API
```

En otra terminal (con un token de admin válido, ver Task 9):

```bash
curl -i -X POST http://localhost:5295/api/inquilinos -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"nombreCompleto":"Test","identificacion":"123","telefono":null,"fotoUrl":null}'
```
Expected: `400 Bad Request` con el mensaje de `IdentificacionEcuatorianaAttribute`. Detener la API (Ctrl+C).

> Nota: los inquilinos dummy del seed (`Identificacion = "1234567"`) no pasan por validación de modelo porque se insertan directo con EF Core, no vía el endpoint — no rompen con este cambio.

- [ ] **Step 8: Commit**

```bash
git add RentaFacil.Shared/Validaciones/IdentificacionEcuatorianaAttribute.cs RentaFacil.Shared/Models/InquilinoDto.cs RentaFacil.Tests/IdentificacionEcuatorianaAttributeTests.cs
git commit -m "feat: validate Ecuadorian cedula/RUC on CrearInquilinoDto"
```

---

### Task 18: Cliente MAUI — `AuthService` real + `DelegatingHandler` con el Bearer token

**Files:**
- Create: `RentaFacil.MAUI/Services/AuthHeaderHandler.cs`
- Modify: `RentaFacil.MAUI/Services/AuthService.cs`
- Modify: `RentaFacil.MAUI/MauiProgram.cs`

(`RentaFacil.MAUI/Services/ApiClient.cs` no se modifica: sigue usando el `HttpClient` que le inyecta el contenedor, que ahora ya lleva el `AuthHeaderHandler` por detrás.)

**Interfaces:**
- Consumes: `POST /api/auth/login` (Task 5), `LoginDto`/`LoginResultDto` (Task 3).
- Produces: `AuthService.{LoginAsync(string, string): Task<bool>, IsAuthenticated, Logout(), OnAuthStateChanged}`; `AuthHeaderHandler : DelegatingHandler` que adjunta el token guardado y dispara `Logout()` ante un `401`.

`AuthService` deja de ser síncrono (`Login`) porque ahora llama a la red — se vuelve `LoginAsync`. Esto es un cambio de firma intencional: cualquier página que lo use debe actualizarse (se hace en la Task 19).

- [ ] **Step 1: Crear `AuthHeaderHandler`**

```csharp
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
```

- [ ] **Step 2: Reescribir `AuthService` con su propio `HttpClient` interno**

`AuthService` construye su propio `HttpClient` en el constructor en vez de recibirlo por DI. Esto evita un problema de ambigüedad: si se registraran dos `HttpClient` distintos en el contenedor (uno para `AuthService`, otro con `AuthHeaderHandler` para `ApiClient`), .NET resuelve por tipo y `AuthService` terminaría recibiendo el último `HttpClient` registrado — que es justamente el que lleva el handler que depende de `AuthService`. Construir el suyo aparte rompe esa ambigüedad de raíz.

```csharp
using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using RentaFacil.MAUI.Config;
using RentaFacil.Shared.Models;

namespace RentaFacil.MAUI.Services;

public class AuthService
{
    private const string TokenKey = "auth_token";
    private const string RolKey = "auth_rol";
    private readonly HttpClient _http;

    public bool IsAuthenticated { get; private set; }
    public string? Rol { get; private set; }

    public event Action? OnAuthStateChanged;

    public AuthService()
    {
#if DEBUG
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
#else
        _http = new HttpClient { BaseAddress = new Uri(ApiConfig.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
#endif
    }

    public async Task InicializarAsync()
    {
        var token = await SecureStorage.GetAsync(TokenKey);
        IsAuthenticated = !string.IsNullOrEmpty(token);
        Rol = await SecureStorage.GetAsync(RolKey);
    }

    public async Task<bool> LoginAsync(string nombreUsuario, string password)
    {
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/auth/login", new LoginDto(nombreUsuario, password));
            if (!respuesta.IsSuccessStatusCode) return false;

            var resultado = await respuesta.Content.ReadFromJsonAsync<LoginResultDto>();
            if (resultado == null) return false;

            SecureStorage.SetAsync(TokenKey, resultado.Token).Wait();
            SecureStorage.SetAsync(RolKey, resultado.Rol).Wait();
            IsAuthenticated = true;
            Rol = resultado.Rol;
            OnAuthStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
            return false;
        }
    }

    public void Logout()
    {
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(RolKey);
        IsAuthenticated = false;
        Rol = null;
        OnAuthStateChanged?.Invoke();
    }
}
```

> Nota: `AuthHeaderHandler` (Step 1) lee el token directamente de `SecureStorage` (no de `AuthService`) para evitar una dependencia circular `AuthService → HttpClient(handler) → AuthService`. `AuthService` sigue siendo la única clase que escribe `TokenKey`/`RolKey`, y la única con autoridad para invalidarlos (`Logout`).

- [ ] **Step 3: Quitar los métodos `Register`/`GetPassword` (ya no aplican — no hay registro público ni recuperación de contraseña en texto plano)**

Confirmar que `AuthService.cs` ya no contiene `Register(...)` ni `GetPassword(...)` después del Step 2 (el archivo completo del Step 2 los reemplaza).

- [ ] **Step 4: Actualizar `MauiProgram.cs` para registrar el `HttpClient` con el handler**

Reemplazar el bloque completo de configuración del `HttpClient`:

```csharp
		// API Client Configuration usando ApiConfig centralizado
		var apiBaseUrl = RentaFacil.MAUI.Config.ApiConfig.BaseUrl;
		
#if DEBUG
		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};
		builder.Services.AddScoped(sp => new HttpClient(handler) 
		{ 
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});
#else
		builder.Services.AddScoped(sp => new HttpClient 
		{ 
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});
#endif

		builder.Services.AddScoped<ApiClient>();
		builder.Services.AddSingleton<AuthService>();
```

Por:

```csharp
		// API Client Configuration usando ApiConfig centralizado
		var apiBaseUrl = RentaFacil.MAUI.Config.ApiConfig.BaseUrl;

		builder.Services.AddSingleton<AuthService>();

#if DEBUG
		var innerHandler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};
#else
		var innerHandler = new HttpClientHandler();
#endif

		builder.Services.AddScoped(sp => new HttpClient(new AuthHeaderHandler(sp.GetRequiredService<AuthService>(), innerHandler))
		{
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(5)
		});

		builder.Services.AddScoped<ApiClient>();
```

> Nota: ahora solo hay **un** `HttpClient` registrado en el contenedor de DI — el `Scoped` con `AuthHeaderHandler`, usado por `ApiClient`. `AuthService` no se resuelve desde ahí: construye su propio `HttpClient` en su constructor (Step 2), así que no hay dos registros del mismo tipo compitiendo por la inyección.

- [ ] **Step 5: Build de la plataforma Android (la que compila en Windows sin SDKs adicionales)**

Run: `dotnet build RentaFacil.MAUI -f net10.0-android`
Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add RentaFacil.MAUI/Services/AuthHeaderHandler.cs RentaFacil.MAUI/Services/AuthService.cs RentaFacil.MAUI/MauiProgram.cs
git commit -m "feat: AuthService talks to the real API with JWT via DelegatingHandler"
```

---

### Task 19: `Login.razor` simplificado + `MainLayout.razor` inicializa la sesión

**Files:**
- Modify: `RentaFacil.MAUI/Components/Pages/Login.razor`
- Modify: `RentaFacil.MAUI/Components/Layout/MainLayout.razor`

**Interfaces:**
- Consumes: `AuthService.{InicializarAsync(), LoginAsync(string, string), IsAuthenticated}` (Task 18).

`Login.razor` pierde las vistas "register" (no hay registro público, ver spec) y "recover" (no se puede mostrar una contraseña hasheada con BCrypt) — ambas dependían de `AuthService.Register`/`GetPassword`, que ya no existen.

- [ ] **Step 1: Reescribir `Login.razor` completo**

```razor
@page "/login"
@layout LoginLayout
@using RentaFacil.MAUI.Services
@inject AuthService Auth
@inject NavigationManager Nav

<div class="login-container">
    <div class="login-card">
        <div class="text-center mb-4">
            <h2 class="fw-bold text-primary">RentaFácil</h2>
            <p class="text-muted">Gestión de Alquileres</p>
        </div>

        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="alert alert-danger p-2 text-center" role="alert">
                @errorMessage
            </div>
        }

        <form @onsubmit="DoLogin">
            <div class="mb-3">
                <label class="form-label text-muted small">Usuario</label>
                <input type="text" class="form-control form-control-lg rounded-3" @bind="username" required />
            </div>
            <div class="mb-4">
                <label class="form-label text-muted small">Contraseña</label>
                <input type="password" class="form-control form-control-lg rounded-3" @bind="password" required />
            </div>
            <button type="submit" class="btn btn-primary w-100 btn-lg rounded-3 shadow-sm mb-3" disabled="@cargando">
                @(cargando ? "Verificando..." : "Iniciar Sesión")
            </button>
        </form>
    </div>
</div>

@code {
    private string username = "";
    private string password = "";
    private string errorMessage = "";
    private bool cargando = false;

    protected override async Task OnInitializedAsync()
    {
        await Auth.InicializarAsync();
        if (Auth.IsAuthenticated)
        {
            Nav.NavigateTo("/");
        }
    }

    private async Task DoLogin()
    {
        errorMessage = "";
        cargando = true;
        var exito = await Auth.LoginAsync(username, password);
        cargando = false;

        if (exito)
        {
            Nav.NavigateTo("/");
        }
        else
        {
            errorMessage = "Usuario o contraseña incorrectos.";
        }
    }
}
```

- [ ] **Step 2: Actualizar `MainLayout.razor` para inicializar la sesión antes de decidir si redirige a `/login`**

Reemplazar:

```csharp
    protected override void OnInitialized()
    {
        Auth.OnAuthStateChanged += StateHasChanged;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !Auth.IsAuthenticated)
        {
            Nav.NavigateTo("/login");
        }
    }
```

Por:

```csharp
    protected override void OnInitialized()
    {
        Auth.OnAuthStateChanged += StateHasChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await Auth.InicializarAsync();
        if (!Auth.IsAuthenticated)
        {
            Nav.NavigateTo("/login");
        }
    }
```

- [ ] **Step 3: Build de la plataforma Android**

Run: `dotnet build RentaFacil.MAUI -f net10.0-android`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add RentaFacil.MAUI/Components/Pages/Login.razor RentaFacil.MAUI/Components/Layout/MainLayout.razor
git commit -m "feat: simplify Login.razor to real auth only, drop fake register/recover"
```

---

### Task 20: Verificación final de la solución completa

**Files:** ninguno (solo verificación).

**Interfaces:** ninguna nueva — esta tarea confirma que todas las anteriores quedaron correctamente integradas.

- [ ] **Step 1: Build completo de la solución**

Run: `dotnet build RentaFacil.slnx`
Expected: `Build succeeded.` (los 4 proyectos: Shared, API, MAUI, Tests)

- [ ] **Step 2: Suite de tests completa**

Run: `dotnet test RentaFacil.Tests`
Expected: todos los tests PASS — `AutenticacionServiceTests`, `ClaimsPrincipalExtensionsTests`, `AuditoriaInterceptorTests`, `InquilinoServiceTests`, `InmuebleServiceTests`, `UnidadServiceTests`, `ContratoServiceTests`, `PagoServiceTests`, `IdentificacionEcuatorianaAttributeTests`.

- [ ] **Step 3: Smoke test manual end-to-end (base de datos limpia)**

```bash
rm RentaFacil.API/rentafacil.db
dotnet run --project RentaFacil.API
```

Checklist (todo en otra terminal mientras la API corre):

- [ ] `curl -i http://localhost:5295/api/inquilinos` → `401 Unauthorized` sin token.
- [ ] Login con el usuario sembrado (`SeedAdmin:Usuario`/`SeedAdmin:Password` de la Task 9) → `200 OK` con un JWT.
- [ ] `GET /api/inquilinos` con el token → `200 OK`, devuelve el inquilino dummy.
- [ ] `POST /api/inquilinos` con `identificacion: "123"` (inválida) → `400 Bad Request`.
- [ ] `POST /api/inquilinos` con `identificacion: "1712345675"` (cédula válida) → `201 Created`.
- [ ] `GET /api/pagos/1/recibo/carta` con el token del admin → `200 OK`, PDF descargado.
- [ ] Registrar un segundo usuario (`POST /api/auth/registrar` con el token del admin) → login con ese usuario → `GET /api/inquilinos` → `[]` (no ve los datos del admin).
- [ ] 11 intentos de login fallidos en menos de un minuto → el intento 11 devuelve `429`.
- [ ] Respuesta de cualquier endpoint incluye `X-Frame-Options: DENY` y `Content-Security-Policy`.

Detener la API (Ctrl+C) al terminar.

- [ ] **Step 4: Verificar que no quedaron cambios sin intención en `rentafacil.db`**

Run: `git status`
Expected: `RentaFacil.API/rentafacil.db` puede aparecer modificado (es esperado, ver `errores-conocidos.md`) — confirmar que no hay otros archivos sin commitear fuera de lo ya commiteado en las Tasks 1-19.

- [ ] **Step 5: Actualizar la sección "Pendiente" / "Último Contexto" de `CLAUDE.md`**

En `CLAUDE.md`, en la sección "Pendiente", quitar el punto 🔴 1 (`Filtrar por UsuarioId`) y el punto 2 (auditoría) y el punto 3 (cabeceras) y el punto 4 (validación de cédula) de la lista de seguridad/auditoría — ya implementados. Reescribir "Último Contexto" con la fecha de hoy, resumiendo que se implementó auth real (JWT + BCrypt), se cerró el IDOR en las 5 entidades, se agregó auditoría, cabeceras de seguridad, rate limiting de login y validación de cédula/RUC.

- [ ] **Step 6: Commit final de la documentación**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md pending/context after security+audit implementation"
```

---
