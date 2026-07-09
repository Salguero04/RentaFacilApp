# Módulo Inquilino + vinculación por QR — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el inquilino tenga su propio portal en la misma app (MAUI/Web): se registra escaneando el QR que genera su arrendador, queda vinculado a su contrato, consulta su contrato/pagos/recibos/consumos/notificaciones y puede **reportar un pago** para que el arrendador lo confirme.

**Architecture:** Se reutiliza TODA la infraestructura actual: mismos hosts, misma RCL (`RentaFacil.UI`), mismo JWT (el claim `Role` ya viaja en el token y `ITokenStore` ya guarda el rol). Se agrega el puente `Inquilino.UsuarioCuentaId` (persona creada por el arrendador ↔ cuenta `Usuario` con rol `Inquilino`), una tabla `CodigosVinculacion` (el QR es un código corto generado por contrato) y una tabla `ReportesPago` (inquilino reporta → arrendador confirma). El lado inquilino consume endpoints nuevos `api/mi/*` con su propio filtrado de seguridad (por cuenta vinculada, no por `UsuarioId` de arrendador). **Toda la API de arrendador existente se restringe por rol** — hoy cualquier autenticado puede llamarla, y con cuentas de inquilinos eso sería un agujero.

**Tech Stack:** lo existente (.NET 10, EF Core, JWT, SignalR, xUnit+Moq+FluentAssertions) + **QRCoder** (API: genera el PNG del QR) + **ZXing.Net.Maui.Controls** (solo MAUI: escaneo con cámara).

## Diagnóstico previo (2026-07-09) — qué existe ya

- ✅ `AppRoles.Inquilino` ya está definido (`RentaFacil.Shared/AppRoles.cs:7`) — pero **nada lo usa**.
- ✅ `NotificacionPendiente` ya se escribe al editar un contrato (`Tipo="ContratoEditado"`, hook explícito "para la futura app del inquilino") con `NotificacionPendienteRepository.GetAllAsync/AddAsync` — falta el endpoint de lectura del inquilino y marcar como leída.
- ✅ El JWT ya lleva `Role`; `AuthService`/`ITokenStore` ya guardan y exponen el rol en el cliente; la RCL comparte pantallas entre MAUI y Web.
- ✅ SignalR (`IDataChangeNotifier`) ya existe — se reutiliza para avisar al arrendador de un reporte de pago nuevo en tiempo real.
- ❌ `Inquilino` no tiene vínculo con `Usuario` (campo nuevo + migración).
- ❌ **Agujero a cerrar:** los controllers de arrendador solo exigen "autenticado" (FallbackPolicy) — un inquilino logueado podría crear/leer recursos como si fuera arrendador. La Tarea 1 lo cierra ANTES de crear cuentas de inquilinos.
- ❌ No hay QR, códigos de vinculación, ni portal del inquilino.

**Sobre "después del login pregunta si es Arrendador o Inquilino":** el rol vive en la cuenta, así que la pregunta ocurre UNA sola vez, en el registro: la pantalla de login gana el enlace "¿Eres inquilino? Regístrate con tu código". Tras cada login, la app enruta sola: rol `Inquilino` → portal `/mi`; `Administrador`/`Propietario` → la app actual. No se pregunta en cada login.

**Relación con el plan de producción** (`docs/contexto/plan-produccion-oracle.md`): independientes; este plan puede ejecutarse antes o después. Si producción ya está desplegada, `update.sh` aplica la migración nueva solo. Los puntos de contacto entre ambos (email para recuperación, roles vs endpoints anónimos, versionado del APK, SignalR por grupos) están listados en la sección "Integración" de ese plan.

## Global Constraints

- Código, comentarios, UI y commits **en español**. Capas `Model → Repository → Service → Controller` estrictas. DTOs `record` en `RentaFacil.Shared/Models/`.
- `FallbackPolicy = RequireAuthenticatedUser`: endpoints públicos nuevos llevan `[AllowAnonymous]` + rate limit `"auth"` si son de registro/auth.
- El inquilino JAMÁS ve datos de otro inquilino ni del arrendador más allá de lo suyo: todo endpoint `api/mi/*` filtra por los `Inquilino` cuyo `UsuarioCuentaId` = id de la cuenta del token.
- El arrendador JAMÁS pierde funcionalidad: sus pantallas/endpoints siguen igual, solo ganan `[Authorize(Roles = "Administrador,Propietario")]`.
- Código de vinculación: 8 caracteres de `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (sin 0/O/1/I), único, expira a los **7 días**, de un solo uso. El QR codifica el código en texto plano.
- Una cuenta de inquilino puede vincularse a varios `Inquilino` (si renta a más de un arrendador); un `Inquilino` solo a una cuenta.
- Reporte de pago: `Monto` decimal(18,2) > 0, `Comentario` opcional (500), `FotoComprobante` opcional `varbinary(max)` con límite **1 MB** validado en API; estados `Pendiente=0 / Confirmado=1 / Rechazado=2`.
- El registro del inquilino captura **email opcional** (se guarda en `Usuario.Email`) — es lo que le permite usar la recuperación de contraseña del plan de producción (`docs/contexto/plan-produccion-oracle.md`, Fase 3).
- SignalR: con cuentas de inquilinos conectadas, `Clients.All` deja de ser aceptable — la Tarea 7b lo reemplaza por **grupos por usuario** (`usuario-{id}`); ningún evento del arrendador llega a clientes de inquilinos ajenos.
- NO buildear `RentaFacil.slnx` (NETSDK1047); verificar por proyecto + `dotnet test RentaFacil.Tests` (hoy 84/84 — no debe bajar).
- Paquetes nuevos permitidos: `QRCoder` SOLO en `RentaFacil.API`; `ZXing.Net.Maui.Controls` SOLO en `RentaFacil.MAUI`.

---

### Task 1: Cerrar la API de arrendador por rol (bloqueante — va primero)

**Files:**
- Modify: `RentaFacil.API/Controllers/InquilinosController.cs`, `InmueblesController.cs`, `OtherControllers.cs` (Contratos/Pagos/Unidades/Recordatorios), `ServiciosController.cs` (clase `MedidoresController`) — atributo a nivel de clase.
- Test: `RentaFacil.Tests/` no cubre controllers (convención del repo) — verificación manual vía Swagger en el paso 3.

**Interfaces:**
- Produces: constante `PoliticaArrendador` reutilizable: `[Authorize(Roles = AppRoles.Administrador + "," + AppRoles.Propietario)]` en cada controller de dominio.

- [ ] **Step 1:** en CADA controller de dominio listado, agregar sobre la clase (junto al `[ApiController]`):

```csharp
[Authorize(Roles = RentaFacil.Shared.AppRoles.Administrador + "," + RentaFacil.Shared.AppRoles.Propietario)]
```
(`using Microsoft.AspNetCore.Authorization;` si falta). `AuthController` NO se toca (login/registro tienen sus propios atributos). Si existe `ConfigController` (plan de producción), tampoco — es anónimo a propósito.

- [ ] **Step 2:** `dotnet build RentaFacil.API/RentaFacil.API.csproj && dotnet test RentaFacil.Tests` → 0 errores, 84/84.
- [ ] **Step 3 (manual):** API corriendo → login normal (cuenta Administrador) → `GET /api/inquilinos` funciona igual (200). Con un token inventado sin rol → 403. Esperado: el arrendador no nota ningún cambio.
- [ ] **Step 4: Commit** — `git commit -m "fix: restringe la API de arrendador a roles Administrador/Propietario"`

### Task 2: Modelo + migración `ModuloInquilino`

**Files:**
- Modify: `RentaFacil.API/Models/Inquilino.cs`
- Create: `RentaFacil.API/Models/CodigoVinculacion.cs`, `RentaFacil.API/Models/ReportePago.cs`
- Create: `RentaFacil.Shared/Enums/EstadoReportePago.cs`
- Modify: `RentaFacil.API/Data/AppDbContext.cs` (DbSets + `OnModelCreating`)
- Migración: `dotnet ef migrations add ModuloInquilino` (desde `RentaFacil.API/`)

**Interfaces:**
- Produces: `Inquilino.UsuarioCuentaId int?`; entidades `CodigoVinculacion` y `ReportePago` (schema `renta`); enum `EstadoReportePago { Pendiente=0, Confirmado=1, Rechazado=2 }` en Shared (lo usan DTOs y UI).

- [ ] **Step 1:** `Inquilino.cs` — agregar:

```csharp
// Cuenta de acceso del inquilino (auth.Usuarios). Null = aún no se ha registrado en la app.
public int? UsuarioCuentaId { get; set; }
```

- [ ] **Step 2:** `EstadoReportePago.cs` (Shared/Enums):

```csharp
namespace RentaFacil.Shared.Enums;

public enum EstadoReportePago { Pendiente = 0, Confirmado = 1, Rechazado = 2 }
```

- [ ] **Step 3:** `CodigoVinculacion.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace RentaFacil.API.Models;

// Código de un solo uso que el arrendador genera por contrato (se muestra como QR).
// El inquilino lo usa para crear su cuenta y quedar vinculado a ese contrato.
public class CodigoVinculacion
{
    public int Id { get; set; }

    [Required, MaxLength(8)]
    public string Codigo { get; set; } = null!;

    public int ContratoId { get; set; }
    public int InquilinoId { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }   // FechaCreacion + 7 días
    public DateTime? UsadoEn { get; set; }          // null = vigente si no expiró

    public int UsuarioId { get; set; }              // arrendador dueño
}
```

- [ ] **Step 4:** `ReportePago.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using RentaFacil.Shared.Enums;

namespace RentaFacil.API.Models;

// "Ya pagué": el inquilino lo reporta desde su portal; el arrendador lo confirma o rechaza.
// Confirmar NO crea el Pago automáticamente: el arrendador lo registra en CrearPago como siempre.
public class ReportePago
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public int InquilinoId { get; set; }

    public decimal Monto { get; set; }

    [MaxLength(500)]
    public string? Comentario { get; set; }

    public byte[]? FotoComprobante { get; set; }    // JPEG/PNG, máx 1 MB (valida el service)

    public DateTime FechaReporte { get; set; }
    public EstadoReportePago Estado { get; set; }

    public int UsuarioId { get; set; }              // arrendador dueño (para su bandeja)
    public int CuentaInquilinoId { get; set; }      // auth.Usuarios que lo reportó
}
```

- [ ] **Step 5:** `AppDbContext` — DbSets `CodigosVinculacion`, `ReportesPago`; en `OnModelCreating` (mismo estilo de las tablas `renta` existentes): tabla `CodigosVinculacion` schema `renta` con índice ÚNICO en `Codigo` e índice en `UsuarioId`; tabla `ReportesPago` schema `renta`, `Monto decimal(18,2)`, índices en `UsuarioId` y `CuentaInquilinoId`; en `Inquilino`, índice (no único) en `UsuarioCuentaId`. FKs de `ContratoId`/`InquilinoId` como referencias sin cascada estricta (mismo criterio informativo que `Recordatorio.ContratoId` — el arrendador puede borrar contratos sin arrastrar el histórico de reportes).
- [ ] **Step 6:** `dotnet ef migrations add ModuloInquilino` → revisar el archivo generado: AddColumn `UsuarioCuentaId` + CreateTable × 2 + índices. NO `database update` (se aplica al arrancar).
- [ ] **Step 7:** build + tests (84/84) + **Commit** — `git commit -m "feat: modelo de vinculación inquilino-cuenta, códigos QR y reportes de pago"`

### Task 3: Repositorios del módulo

**Files:**
- Create: `RentaFacil.API/Repositories/Interfaces/IPortalInquilinoRepositories.cs`
- Create: `RentaFacil.API/Repositories/PortalInquilinoRepositories.cs`
- Modify: `RentaFacil.API/Program.cs` (DI Scoped, junto a los repos existentes)

**Interfaces:**
- Produces (las firmas EXACTAS que consumen Tasks 4-7):

```csharp
public interface ICodigoVinculacionRepository
{
    Task<CodigoVinculacion> AddAsync(CodigoVinculacion codigo);
    Task<CodigoVinculacion?> GetVigenteAsync(string codigo);      // no usado y no expirado
    Task UpdateAsync(CodigoVinculacion codigo);
}

public interface IReportePagoRepository
{
    Task<ReportePago> AddAsync(ReportePago reporte);
    Task<IEnumerable<ReportePago>> GetByArrendadorAsync(int usuarioId);
    Task<IEnumerable<ReportePago>> GetByCuentaInquilinoAsync(int cuentaInquilinoId);
    Task<ReportePago?> GetByIdAsync(int id, int usuarioId);        // ownership arrendador
    Task UpdateAsync(ReportePago reporte);
}

public interface IPortalInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetInquilinosPorCuentaAsync(int cuentaId);        // UsuarioCuentaId == cuentaId
    Task<IEnumerable<Contrato>> GetContratosPorInquilinosAsync(List<int> inquilinoIds);
    Task<IEnumerable<Pago>> GetPagosPorContratosAsync(List<int> contratoIds);
    Task<IEnumerable<MedidorInquilino>> GetVinculosMedidorPorInquilinosAsync(List<int> inquilinoIds); // Include(Medidor)
    Task<IEnumerable<NotificacionPendiente>> GetNotificacionesPorInquilinosAsync(List<int> inquilinoIds);
    Task<NotificacionPendiente?> GetNotificacionAsync(int id);
    Task MarcarNotificadaAsync(NotificacionPendiente notificacion);
}
```

- [ ] **Step 1:** crear interfaz + implementación (EF directo, mismo estilo de `OtherRepositories.cs`; `GetVigenteAsync`: `Codigo == codigo && UsadoEn == null && FechaExpiracion > DateTime.UtcNow`). **OJO:** los métodos de `IPortalInquilinoRepository` NO filtran por `UsuarioId` de arrendador — su seguridad es la lista de `inquilinoIds`/`cuentaId` que el Service deriva del token. Comentarlo así en el código.
- [ ] **Step 2:** registrar los 3 en DI. Build + commit — `git commit -m "feat: repositorios de vinculación, reportes de pago y portal del inquilino"`

### Task 4: Servicio de vinculación + registro de cuenta inquilino (TDD)

**Files:**
- Modify: `RentaFacil.Shared/Models/AuthDto.cs` (+`RegistrarInquilinoDto`)
- Create: `RentaFacil.Shared/Models/PortalInquilinoDtos.cs` (DTOs del módulo; ver Interfaces)
- Create: `RentaFacil.API/Services/Interfaces/IVinculacionService.cs` + `RentaFacil.API/Services/VinculacionService.cs`
- Modify: `RentaFacil.API/Program.cs` (DI)
- Test: `RentaFacil.Tests/VinculacionServiceTests.cs`

**Interfaces:**
- Consumes: `ICodigoVinculacionRepository`, `IContratoRepository.GetByIdAsync(id, usuarioId)` (existe), `IInquilinoRepository.GetByIdAsync/UpdateAsync` (existen), `IUsuarioRepository.GetByNombreUsuarioAsync/AddAsync` (existen), la emisión de JWT de `IAutenticacionService`.
- Produces:

```csharp
// Email opcional: habilita la recuperación de contraseña por correo (plan de producción, Fase 3)
public record RegistrarInquilinoDto(string Codigo, string NombreUsuario, string Password, string? Email);   // en AuthDto.cs
public record CodigoVinculacionDto(string Codigo, DateTime FechaExpiracion);                     // en PortalInquilinoDtos.cs

public interface IVinculacionService
{
    // Arrendador: genera código para un contrato suyo (null si el contrato no es suyo).
    Task<CodigoVinculacionDto?> GenerarCodigoAsync(int contratoId, int usuarioId);
    // Público: crea cuenta rol Inquilino y vincula. Errores tipados para el controller.
    Task<(LoginResultDto? Resultado, string? Error)> RegistrarInquilinoAsync(RegistrarInquilinoDto dto);
    // Inquilino ya logueado que agrega otro contrato/arrendador con un código nuevo.
    Task<bool> VincularCuentaExistenteAsync(string codigo, int cuentaId);
    byte[] GenerarQrPng(string codigo);   // QRCoder, PNG 20px/módulo
}
```

- [ ] **Step 1:** agregar paquete: `dotnet add RentaFacil.API package QRCoder`
- [ ] **Step 2: tests que fallan** (estilo Moq del repo; los esenciales):

```csharp
[Fact] // el código se genera con el formato y expiración pactados
public async Task GenerarCodigo_ContratoPropio_Genera8CharsSinAmbiguosYExpira7Dias()
[Fact]
public async Task GenerarCodigo_ContratoAjeno_DevuelveNull()
[Fact]
public async Task RegistrarInquilino_CodigoInexistenteOExpiradoOUsado_DevuelveError()   // GetVigenteAsync → null
[Fact]
public async Task RegistrarInquilino_NombreUsuarioTomado_DevuelveError()
[Fact] // camino feliz: crea Usuario rol Inquilino, setea Inquilino.UsuarioCuentaId, marca código usado, devuelve token
public async Task RegistrarInquilino_CodigoVigente_CreaCuentaVinculaYDevuelveToken()
[Fact]
public async Task VincularCuentaExistente_CodigoVigente_SeteaUsuarioCuentaIdYMarcaUsado()
[Fact] // un inquilino ya vinculado a OTRA cuenta no se puede re-vincular
public async Task RegistrarInquilino_InquilinoYaVinculadoAOtraCuenta_DevuelveError()
```
Verificaciones clave con `Verify`: `AddAsync` de Usuario con `Rol == AppRoles.Inquilino`, `PasswordHash` BCrypt válido y `Email == dto.Email` (null si no lo dio); `UpdateAsync` del Inquilino con `UsuarioCuentaId` seteado; `UpdateAsync` del código con `UsadoEn != null`.

- [ ] **Step 3:** correr → FAIL. **Step 4: implementar.** Puntos no obvios:
  - Generación del código: `Random.Shared` sobre el alfabeto `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`; reintentar si `GetVigenteAsync` (o el índice único al insertar) choca.
  - `RegistrarInquilinoAsync`: password mínimo 8; el `Usuario` nuevo va con `Activo=true`, `FechaCreacion=DateTime.UtcNow`; para emitir el token reutilizar `IAutenticacionService` (exponer ahí `LoginResultDto GenerarTokenParaCuenta(Usuario usuario)` público-interno o duplicar la emisión NO — reutilizar; el refactor `GenerarToken` privado ya existe, promoverlo a un método de interfaz `EmitirToken(Usuario)` usado solo server-side).
  - `GenerarQrPng`: `using var qr = new QRCodeGenerator(); var data = qr.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.M); return new PngByteQRCode(data).GetGraphic(20);`
- [ ] **Step 5:** correr → PASS (84 + 7). **Step 6: Commit** — `git commit -m "feat: servicio de vinculación con código QR y registro self-service de inquilinos"`

### Task 5: Endpoints de vinculación

**Files:**
- Modify: `RentaFacil.API/Controllers/OtherControllers.cs` (clase `ContratosController`: generar código/QR)
- Modify: `RentaFacil.API/Controllers/AuthController.cs` (registro inquilino)

**Interfaces:**
- Consumes: `IVinculacionService` (Task 4).
- Produces (los consume la UI en Tasks 8-10):
  - `POST api/contratos/{id}/codigo-vinculacion` → 200 `CodigoVinculacionDto` | 404 (rol arrendador — el controller ya quedó cerrado en Task 1)
  - `GET api/contratos/codigo-vinculacion/{codigo}/qr` → 200 `image/png` (rol arrendador)
  - `POST api/auth/registrar-inquilino` → 200 `LoginResultDto` | 400 `{ message }` — `[AllowAnonymous]` + `[EnableRateLimiting("auth")]`
  - `POST api/mi/vincular` se define en Task 6 (vive en `MiPortalController`).

- [ ] **Step 1:** en `ContratosController`:

```csharp
[HttpPost("{id}/codigo-vinculacion")]
public async Task<IActionResult> GenerarCodigoVinculacion(int id)
{
    var dto = await _vinculacionService.GenerarCodigoAsync(id, User.ObtenerUsuarioId());
    if (dto == null) return NotFound();
    return Ok(dto);
}

[HttpGet("codigo-vinculacion/{codigo}/qr")]
public IActionResult ObtenerQr(string codigo) =>
    File(_vinculacionService.GenerarQrPng(codigo), "image/png");
```

- [ ] **Step 2:** en `AuthController`: `RegistrarInquilino([FromBody] RegistrarInquilinoDto dto)` → si `Error != null` → `BadRequest(new { message = error })`; si no → `Ok(resultado)`.
- [ ] **Step 3:** build + prueba Swagger (generar código con el admin → recibir 8 chars; `registrar-inquilino` con ese código → 200 con token cuyo rol es Inquilino). **Commit.**

### Task 6: Servicio + endpoints del portal (`api/mi/*`) (TDD)

**Files:**
- Modify: `RentaFacil.Shared/Models/PortalInquilinoDtos.cs`
- Create: `RentaFacil.API/Services/Interfaces/IPortalInquilinoService.cs` + `RentaFacil.API/Services/PortalInquilinoService.cs`
- Create: `RentaFacil.API/Controllers/MiPortalController.cs`
- Modify: `RentaFacil.API/Program.cs` (DI)
- Test: `RentaFacil.Tests/PortalInquilinoServiceTests.cs`

**Interfaces:**
- Consumes: `IPortalInquilinoRepository` (Task 3), `IVinculacionService.VincularCuentaExistenteAsync` (Task 4), `IReciboService` existente (para el PDF del recibo).
- Produces (DTOs en `PortalInquilinoDtos.cs` — reutilizan enums existentes):

```csharp
public record MiContratoDto(int ContratoId, string NombreArrendador, string NombreUnidad, string NombreInmueble,
                            decimal Monto, FrecuenciaPago Frecuencia, int DiaPago, DateTime FechaInicio, DateTime FechaFin, bool Activo);
public record MiPagoDto(int PagoId, int ContratoId, string Periodo, decimal TotalMonto, decimal ACuenta,
                        decimal Servicios, DateTime FechaPago, bool Completado);
public record MiConsumoDto(string NombreMedidor, TipoServicio Tipo, decimal LecturaAnterior, decimal LecturaActual,
                           MetodoCobroInquilino MetodoCobro);
public record MiNotificacionDto(int Id, string Tipo, string? Detalle, DateTime Fecha, bool Notificado);
```
Endpoints (todos `[Authorize(Roles = AppRoles.Inquilino)]`, controller `[Route("api/mi")]`):
`GET api/mi/contratos` · `GET api/mi/pagos` · `GET api/mi/pagos/{id}/recibo?formato=` (reusa `IReciboService`, 404 si el pago no es de sus contratos) · `GET api/mi/consumos` · `GET api/mi/notificaciones` · `PUT api/mi/notificaciones/{id}/leida` · `POST api/mi/vincular` body `{ codigo }`.
El id de cuenta sale del token: `User.ObtenerUsuarioId()` (mismo helper existente — para una cuenta inquilino ese claim ES su cuenta).

- [ ] **Step 1: tests que fallan** — los de seguridad son los importantes:

```csharp
[Fact] public async Task GetPagos_SoloDevuelvePagosDeContratosDeSusInquilinosVinculados()
[Fact] public async Task GetPagos_CuentaSinVinculos_DevuelveVacio()
[Fact] public async Task GetReciboPago_PagoDeOtroInquilino_DevuelveNull()          // el service devuelve null → 404
[Fact] public async Task MarcarNotificacionLeida_DeOtroInquilino_DevuelveFalse()
[Fact] public async Task GetContratos_MapeaNombreArrendadorInmuebleYUnidad()
```

- [ ] **Step 2:** FAIL → implementar → PASS. El flujo interno de cada método: `GetInquilinosPorCuentaAsync(cuentaId)` → ids → repos → mapear DTOs. Para `NombreArrendador`: el `Usuario` arrendador via `IUsuarioRepository.GetByIdAsync(inquilino.UsuarioId)` → `NombreUsuario`.
- [ ] **Step 3:** `MiPortalController` — delgado, patrón de los demás (`NotFound()`/`NoContent()`/`Ok(...)`).
- [ ] **Step 4:** build + tests + **Commit** — `git commit -m "feat: portal del inquilino: contratos, pagos, recibos, consumos y notificaciones"`

### Task 7: Reportes de pago — servicio + endpoints + notificación en tiempo real (TDD)

**Files:**
- Modify: `RentaFacil.Shared/Models/PortalInquilinoDtos.cs` (+DTOs de reporte)
- Create: `RentaFacil.API/Services/Interfaces/IReportePagoService.cs` + `RentaFacil.API/Services/ReportePagoService.cs`
- Modify: `RentaFacil.API/Controllers/MiPortalController.cs` (crear/listar del inquilino) y `OtherControllers.cs` → `PagosController` o controller nuevo `ReportesPagoController` (bandeja del arrendador)
- Modify: `RentaFacil.API/Program.cs` (DI)
- Test: `RentaFacil.Tests/ReportePagoServiceTests.cs`

**Interfaces:**
- Consumes: `IReportePagoRepository` (Task 3), `IPortalInquilinoRepository`, `IDataChangeNotifier` (existente — SignalR).
- Produces:

```csharp
public record CrearReportePagoDto(int ContratoId, decimal Monto, string? Comentario, byte[]? FotoComprobante);
public record ReportePagoDto(int Id, int ContratoId, int InquilinoId, string NombreInquilino, decimal Monto,
                             string? Comentario, bool TieneComprobante, DateTime FechaReporte, EstadoReportePago Estado);
```
Endpoints inquilino (en `MiPortalController`): `POST api/mi/reportes-pago` (valida: contrato pertenece a sus vínculos, monto > 0, foto ≤ 1 MB) → 201; `GET api/mi/reportes-pago` → sus reportes.
Endpoints arrendador (`ReportesPagoController`, `[Authorize(Roles=...arrendador)]`, ruta `api/reportes-pago`): `GET` (bandeja, con filtro `?estado=`), `GET {id}/comprobante` (PNG/JPEG bytes o 404), `PUT {id}/confirmar`, `PUT {id}/rechazar` → 204/404.
Al CREAR un reporte: `IDataChangeNotifier.NotificarCambioAsync("ReportePago", usuarioIdArrendador, "crear")` — el arrendador lo ve llegar en tiempo real con la infraestructura SignalR ya montada.

- [ ] **Step 1: tests que fallan:**

```csharp
[Fact] public async Task CrearReporte_ContratoNoVinculadoASuCuenta_DevuelveNull()
[Fact] public async Task CrearReporte_FotoMayorA1MB_DevuelveNull()
[Fact] public async Task CrearReporte_Valido_PersisteConEstadoPendienteYNotificaPorSignalR()   // Verify NotificarCambioAsync Times.Once
[Fact] public async Task Confirmar_ReporteDeOtroArrendador_DevuelveFalse()
[Fact] public async Task Confirmar_ReportePendiente_CambiaEstadoADevuelveTrue()
[Fact] public async Task Rechazar_ReportePendiente_CambiaEstado()
[Fact] public async Task Confirmar_ReporteYaConfirmado_DevuelveFalse()   // no re-transicionar
```

- [ ] **Step 2:** FAIL → implementar → PASS (84 + 7 + 5 + ~7 ≈ 103). **Step 3:** controllers + build + **Commit** — `git commit -m "feat: reportes de pago del inquilino con confirmación del arrendador y aviso SignalR"`

### Task 7b: SignalR por grupos de usuario (reemplaza `Clients.All`)

> Con cuentas de inquilinos conectadas al mismo hub, `Clients.All` filtraría eventos del arrendador a clientes ajenos (solo metadatos — entidad/usuarioId/acción — pero es ruido y una fuga de actividad). Esto cierra el minor anotado en la revisión de la Fase 2 de SignalR ("agrupar antes de multiusuario").

**Files:**
- Modify: `RentaFacil.API/Hubs/DatosHub.cs`
- Modify: `RentaFacil.API/Services/DataChangeNotifier.cs`
- (Sin cambios en `SignalRClient` ni en las páginas: el nombre del evento y su payload no cambian.)

**Interfaces:**
- Consumes: claim `NameIdentifier` del token (ya presente en todos los JWT).
- Produces: cada conexión queda en el grupo `usuario-{id}`; `NotificarCambioAsync(entidad, usuarioId, accion)` emite SOLO a `Clients.Group($"usuario-{usuarioId}")`. El `usuarioId` que ya reciben los llamadores es el del **destinatario** (en reportes de pago, el arrendador — Task 7 ya lo pasa así).

- [ ] **Step 1:** `DatosHub` — agregar:

```csharp
public override async Task OnConnectedAsync()
{
    // Cada cliente solo escucha su propio grupo: los eventos del arrendador
    // no llegan a los inquilinos (ni a otros usuarios) y viceversa.
    var id = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrEmpty(id))
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario-{id}");
    await base.OnConnectedAsync();
}
```

- [ ] **Step 2:** `DataChangeNotifier.NotificarCambioAsync` — cambiar `Clients.All.SendAsync(...)` por `Clients.Group($"usuario-{usuarioId}").SendAsync(...)` (mismo try/catch best-effort existente, sin tocar la firma).
- [ ] **Step 3:** `dotnet build RentaFacil.API/RentaFacil.API.csproj && dotnet test RentaFacil.Tests` → verdes (los tests del notifier usan el mock de `IDataChangeNotifier`, no cambian).
- [ ] **Step 4 (manual, con 2 sesiones del MISMO arrendador):** registrar un pago en una → la otra refresca en vivo igual que antes (los dos clientes comparten grupo). Con una sesión de inquilino abierta a la vez: NO recibe ese evento.
- [ ] **Step 5: Commit** — `git commit -m "feat: eventos SignalR agrupados por usuario (aisla arrendadores e inquilinos)"`

### Task 8: ApiClient + enrutado por rol en la UI

**Files:**
- Modify: `RentaFacil.UI/Services/ApiClient.cs` (métodos nuevos: generar código, QR url, registrar inquilino, todo `api/mi/*`, reportes)
- Modify: `RentaFacil.UI/Services/AuthService.cs` (exponer `EsInquilino => Rol == AppRoles.Inquilino`)
- Modify: `RentaFacil.UI/Layout/MainLayout.razor` + `RentaFacil.UI/Layout/NavMenu.razor`
- Modify: `RentaFacil.UI/Pages/Login.razor` (redirección post-login por rol + enlace de registro)

**Interfaces:**
- Consumes: endpoints de Tasks 5-7; `AuthService.Rol` ya existe (lo puebla `ITokenStore`).
- Produces: `ApiClient` con: `GenerarCodigoVinculacionAsync(contratoId)` → `CodigoVinculacionDto?`; `UrlQr(codigo)` → string absoluta para `<img src>`; `RegistrarInquilinoAsync(RegistrarInquilinoDto)` → `(LoginResultDto?, string? error)`; `GetMisContratosAsync()`, `GetMisPagosAsync()`, `GetMisConsumosAsync()`, `GetMisNotificacionesAsync()`, `MarcarNotificacionLeidaAsync(id)`, `VincularCodigoAsync(codigo)`, `CrearReportePagoAsync(dto)`, `GetMisReportesPagoAsync()`, `GetReportesPagoAsync()`, `ConfirmarReporteAsync(id)`, `RechazarReporteAsync(id)`, `GetComprobanteUrl(id)`.

- [ ] **Step 1:** métodos en `ApiClient` (mismo patrón GET/POST/PUT existente; `UrlQr` = `BaseAddress + $"api/contratos/codigo-vinculacion/{codigo}/qr"` — nota: `<img>` no manda el Bearer; por eso este GET de imagen se sirve con el código como secreto en la URL y expira con él. Documentar en comentario).

  **OJO seguridad imagen QR:** como `<img src>` no lleva token, cambiar el endpoint del QR de Task 5 a `[AllowAnonymous]` es INACEPTABLE… la alternativa correcta y simple: `ApiClient.GetQrPngAsync(codigo)` → `byte[]` (HttpClient CON Bearer) y la UI lo muestra como `data:image/png;base64,...`. Implementar ASÍ (nada anónimo).
- [ ] **Step 2:** `Login.razor` — tras login OK: `Nav.NavigateTo(Auth.EsInquilino ? "/mi" : "/")`. Debajo del formulario: enlace "¿Eres inquilino? Regístrate con tu código" → `/registro-inquilino`.
- [ ] **Step 3:** `NavMenu.razor` — envolver las entradas actuales en `@if (!Auth.EsInquilino) { ...actuales... } else { Mi contrato(/mi) · Mis pagos(/mi/pagos) · Consumos(/mi/consumos) · Notificaciones(/mi/notificaciones) · Reportar pago(/mi/reportar) }` con los mismos estilos/`AlNavegar`. `MainLayout` no cambia de estructura (solo si el chequeo de versión del otro plan ya vive ahí, conviven).
- [ ] **Step 4:** builds UI/Web + **Commit** — `git commit -m "feat: ApiClient del portal inquilino y enrutado de la UI por rol"`

### Task 9: Pantallas del inquilino (RCL)

**Files:**
- Create: `RentaFacil.UI/Pages/RegistroInquilino.razor` (`@page "/registro-inquilino"`, layout `LoginLayout`, público)
- Create: `RentaFacil.UI/Pages/MiPortal.razor` (`@page "/mi"`)
- Create: `RentaFacil.UI/Pages/MisPagos.razor` (`@page "/mi/pagos"`)
- Create: `RentaFacil.UI/Pages/MisConsumos.razor` (`@page "/mi/consumos"`)
- Create: `RentaFacil.UI/Pages/MisNotificaciones.razor` (`@page "/mi/notificaciones"`)
- Create: `RentaFacil.UI/Pages/ReportarPago.razor` (`@page "/mi/reportar"`)

**Interfaces:**
- Consumes: `ApiClient` (Task 8), `IDispositivoServicio.GuardarArchivoAsync` (existente, para el PDF del recibo), `IEscanerQr` (Task 10 — en `RegistroInquilino` usar `try`: si aún no existe al implementar esta task, dejar solo el input manual y un TODO NO — implementar Task 10 antes que esta si se ejecuta secuencial; el plan las ordena 10 después solo por aislar el paquete nativo: **el implementador de esta task usa la interfaz `IEscanerQr` ya definida en Task 10-Step 1, que se adelanta a `RentaFacil.UI/Abstractions` aquí si no existe**).
- Produces: el flujo completo del inquilino en ambos clientes.

- [ ] **Step 1:** `RegistroInquilino.razor` — campos: código (8 chars, mayúsculas automáticas), botón "Escanear QR" visible solo si `EscanerQr.EstaSoportado`, usuario, contraseña (mín 8) + confirmar, y **email opcional** (con la nota "para poder recuperar tu contraseña"); al enviar `Api.RegistrarInquilinoAsync` → si OK, el resultado trae token: guardar vía `AuthService` (agregar ahí `IniciarSesionConResultadoAsync(LoginResultDto)` que reuse el guardado de token/rol) y `Nav.NavigateTo("/mi")`; si error, mostrar `message` de la API. Estilo visual: copiar `Login.razor`.
- [ ] **Step 2:** `MiPortal.razor` — tarjetas por contrato (`GetMisContratosAsync`): arrendador, inmueble/unidad, monto (`MoneyFormatter.Mostrar`), día de pago, estado Activo; sección "código nuevo" → input + `VincularCodigoAsync` para agregar otro contrato; badge con notificaciones sin leer.
- [ ] **Step 3:** `MisPagos.razor` — lista de `MiPagoDto` (periodo, total, a cuenta, saldo = `Math.Max(0, Total-ACuenta)`, `Completado` con barra verde — mismos indicadores del glosario); botón "Recibo" → `GET api/mi/pagos/{id}/recibo?formato=ticket` vía `ApiClient` (bytes) + `IDispositivoServicio.GuardarArchivoAsync(...)` (patrón exacto de descarga de recibo ya usado en las páginas del arrendador — copiarlo).
- [ ] **Step 4:** `MisConsumos.razor` — tabla simple de `MiConsumoDto`: medidor, tipo (iconos `IconoServicio` — copiar el helper de `CrearPago.razor`), lecturas, método de cobro.
- [ ] **Step 5:** `MisNotificaciones.razor` — lista (tipo + detalle + fecha, negrita si `!Notificado`); al tocar → `MarcarNotificacionLeidaAsync` + refrescar.
- [ ] **Step 6:** `ReportarPago.razor` — select de contrato (sus contratos), monto (`type="number"`), comentario, foto opcional: `InputFile` (accept="image/*"; leer stream, límite 1 MB con mensaje claro; en MAUI Blazor Hybrid `InputFile` abre el picker nativo — suficiente, NO usar MediaPicker) → `CrearReportePagoAsync` → confirmación + histórico de sus reportes con estado (Pendiente/Confirmado/Rechazado con colores).
- [ ] **Step 7:** builds + **Commit** — `git commit -m "feat: pantallas del portal inquilino (registro, contratos, pagos, consumos, notificaciones, reportar pago)"`

### Task 10: Escáner QR (MAUI) + abstracción

**Files:**
- Create: `RentaFacil.UI/Abstractions/IEscanerQr.cs`
- Create: `RentaFacil.UI/Services/EscanerQrNoSoportado.cs`
- Create: `RentaFacil.MAUI/Platform/MauiEscanerQr.cs` + `RentaFacil.MAUI/Platform/PaginaEscanerQr.cs` (ContentPage nativa)
- Modify: `RentaFacil.MAUI/RentaFacil.MAUI.csproj` (+`ZXing.Net.Maui.Controls`), `RentaFacil.MAUI/MauiProgram.cs` (`.UseBarcodeReader()` + DI), `RentaFacil.MAUI/Platforms/Android/AndroidManifest.xml` (permiso `CAMERA`)
- Modify: `RentaFacil.Web/Program.cs` (registrar `EscanerQrNoSoportado`)

**Interfaces:**
- Produces:

```csharp
namespace RentaFacil.UI.Abstractions;

/// <summary>Escaneo de códigos QR con la cámara. MAUI → ZXing; Web → no soportado (código manual).</summary>
public interface IEscanerQr
{
    bool EstaSoportado { get; }
    Task<string?> EscanearAsync();   // null si el usuario cancela o no hay permiso
}
```

- [ ] **Step 1:** interfaz + `EscanerQrNoSoportado` (`EstaSoportado => false`) en la RCL; registrar el no-soportado en Web (Scoped) — mismo patrón que `ProveedorGoogleNoSoportado`.
- [ ] **Step 2:** `dotnet add RentaFacil.MAUI package ZXing.Net.Maui.Controls`; en `MauiProgram.cs`: `.UseBarcodeReader()` en el builder; permiso cámara en el manifest Android: `<uses-permission android:name="android.permission.CAMERA" />`.
- [ ] **Step 3:** `PaginaEscanerQr.cs` — `ContentPage` con `CameraBarcodeReaderView` (formato QR only, autostart) y un `TaskCompletionSource<string?>`; en `BarcodesDetected` → set result + `Navigation.PopModalAsync`; botón Cancelar → result null. `MauiEscanerQr.EscanearAsync()`: `Permissions.RequestAsync<Permissions.Camera>()` → si denegado null; `Application.Current.MainPage.Navigation.PushModalAsync(pagina)` → await TCS. Registrar `MauiEscanerQr` Singleton.
- [ ] **Step 4:** `dotnet build RentaFacil.MAUI -f net10.0-android` → 0 errores. Verificar builds UI/Web. **Commit** — `git commit -m "feat: escaneo de QR con cámara en MAUI (ZXing) detrás de IEscanerQr"`

### Task 11: Pantallas del arrendador — QR del contrato + bandeja de reportes

**Files:**
- Modify: `RentaFacil.UI/Pages/Contratos.razor` (acción "Vincular inquilino (QR)")
- Create: `RentaFacil.UI/Pages/ReportesPago.razor` (`@page "/reportes-pago"`)
- Modify: `RentaFacil.UI/Layout/NavMenu.razor` (entrada "Reportes de pago" con `bi-inbox`, solo rama arrendador)

**Interfaces:**
- Consumes: `ApiClient.GenerarCodigoVinculacionAsync/GetQrPngAsync` (Task 8), `GetReportesPagoAsync/Confirmar/Rechazar/GetComprobanteUrl→bytes`, `SignalRClient.CambioDatos` (existente, entidad `"ReportePago"`).

- [ ] **Step 1:** `Contratos.razor` — en la tarjeta/acciones del contrato, botón "Vincular inquilino" → modal (estilo `modal-custom` existente): llama `GenerarCodigoVinculacionAsync(contrato.Id)` → muestra el QR (`<img src="data:image/png;base64,@qrBase64">`), el código en texto grande (por si lo escriben a mano) y la expiración ("válido hasta …", `MoneyFormatter` NO — es fecha: `ToString("dd/MMM HH:mm")`).
- [ ] **Step 2:** `ReportesPago.razor` — lista de `ReportePagoDto` pendientes (nombre inquilino, monto, comentario, fecha, botón "Ver comprobante" si `TieneComprobante` → bytes → mostrar en modal como data URL); botones Confirmar (verde) / Rechazar (rojo) con confirmación; tras confirmar, mostrar acceso directo "Registrar pago" → `Nav.NavigateTo($"/crearpago/{reporte.ContratoId}")`. Suscribirse a `SignalRClient.CambioDatos` filtrando `entidad == "ReportePago"` para refrescar en vivo (patrón EXACTO de `Pagos.razor`, incluido `InvokeAsync` + try/catch + `Dispose` con `-=`).
- [ ] **Step 3:** builds + **Commit** — `git commit -m "feat: QR de vinculación en contratos y bandeja de reportes de pago del arrendador"`

### Task 12: Docs (regla de cierre de CLAUDE.md)

- [ ] `CLAUDE.md`: "Último Contexto" (reescribir con este módulo) y "Pendiente" (el bullet de multiusuario/app del inquilino se actualiza: el módulo inquilino v1 existe).
- [ ] `docs/contexto/arquitectura.md`: entidades nuevas (`CodigoVinculacion`, `ReportePago`, `Inquilino.UsuarioCuentaId`) en el mapa/diagrama; la frase "En una frase" ya no es "de un solo usuario" — ahora hay dos perfiles (arrendador e inquilino de solo-su-data).
- [ ] `docs/contexto/glosario.md`: términos nuevos (Código de vinculación, Reporte de pago, Portal del inquilino, Cuenta de inquilino).
- [ ] `docs/contexto/decisiones.md`: entrada nueva "Módulo inquilino: vinculación por código QR de un solo uso + endpoints api/mi/* filtrados por cuenta" (decisión, por qué, descartados: deep links, cuenta creada por el arrendador).
- [ ] `docs/contexto/errores-conocidos.md`: si quedó alguna limitación (ej. "la bandeja de reportes no pagina"), anotarla honesta.
- [ ] grep de "app del inquilino"/"NotificacionPendiente" en todos los docs: las notas "no hay consumidor todavía" pasan a "ya RESUELTO (fecha): el portal del inquilino la lee vía api/mi/notificaciones".
- [ ] `CLAUDE.md` gotcha (g) "eventos SignalR van a Clients.All sin agrupar" → marcar resuelto por la Tarea 7b (grupos por usuario).
- [ ] **Commit** — `git commit -m "docs: documenta el módulo inquilino y cierra las notas de 'futura app del inquilino'"`

---

## Verificación end-to-end

1. `dotnet build` API/UI/Web + `dotnet build RentaFacil.MAUI -f net10.0-android` + `dotnet test RentaFacil.Tests` → ~103+ tests verdes.
2. **Flujo completo con 2 cuentas (manual):**
   a. Login arrendador → Contratos → "Vincular inquilino" → aparece QR + código.
   b. En otro cliente (o sesión), `/registro-inquilino` → escribir el código (o escanearlo en Android) → crear usuario/contraseña → entra directo a `/mi` y ve SU contrato con el nombre del arrendador.
   c. El inquilino ve sus pagos y descarga un recibo PDF; ve sus consumos de medidor; ve la notificación "ContratoEditado" si el arrendador edita el contrato (el hook `NotificacionPendiente` por fin tiene consumidor).
   d. El inquilino reporta un pago con foto → al arrendador le aparece EN VIVO en `/reportes-pago` (SignalR) → confirma → registra el pago real en CrearPago → el inquilino lo ve en "Mis pagos".
   e. **Seguridad:** con el token del inquilino, llamar a mano `GET /api/inquilinos` → **403**; `GET /api/mi/pagos` de una cuenta sin vínculos → lista vacía; reutilizar el código ya usado → error; código expirado (manipular `FechaExpiracion` en BD) → error; con una sesión de inquilino conectada al hub, registrar un pago como arrendador → el inquilino NO recibe el evento SignalR (grupos por usuario, Tarea 7b).
   f. Login del arrendador de siempre: TODO su flujo actual intacto (regresión).
3. Migración `ModuloInquilino` aplicó limpio al arrancar (ver logs).

## Fuera de alcance (explícito)

- Push notifications reales al teléfono del inquilino (la notificación es in-app; el backlog de push sigue en `CLAUDE.md`).
- Que confirmar un reporte cree el `Pago` automáticamente (el arrendador lo registra en `CrearPago`, que ya calcula servicios/medidores correctamente — evitar duplicar esa lógica).
- Chat arrendador↔inquilino, edición de datos del inquilino desde su portal, multi-idioma.
- Revocación/regeneración masiva de códigos (generar uno nuevo simplemente deja el anterior utilizable hasta expirar — anotado en docs si molesta).
