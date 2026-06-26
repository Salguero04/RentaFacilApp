# Diseño: Seguridad real + Auditoría — RentaFacil

**Fecha:** 2026-06-26
**Estado:** aprobado, pendiente de plan de implementación

## Contexto

RentaFacil hoy es de un solo usuario, sin autenticación de servidor: `RentaFacil.MAUI/Services/AuthService.cs` valida contra `Preferences` local (`admin/admin` hardcodeado) y nunca llama a la API. La API no tiene `[Authorize]` en ningún Controller. Todas las entidades (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`) tienen o heredan un `UsuarioId`, pero ningún `Repository`/`Service` lo usa para filtrar — es un IDOR/BOLA confirmado leyendo el código (`InquilinoService.GetAllAsync`/`GetByIdAsync`, `InmuebleService`, `OtherServices.cs`).

Filtrar por `UsuarioId` sin antes tener autenticación real no protege nada (el cliente elige el `UsuarioId` libremente en el DTO). Por eso este diseño implementa **primero auth de servidor, y sobre esa base** el filtro IDOR, auditoría, cabeceras HTTP, rate limiting de login y validación de identificación ecuatoriana.

Este spec cubre **seguridad y auditoría únicamente**. La revisión de UX/UI es un spec separado.

## Modelo de actores (referencia para roles futuros)

Para ubicar el alcance de este spec dentro del modelo de actores completo que maneja el producto:

**Actores primarios (implementados en este spec, como roles):**
- **Administrador** — gestiona todo el sistema (propiedades, contratos, pagos).
- **Propietario** — registra inmuebles, revisa ingresos, aprueba contratos.
- **Inquilino** — consulta su contrato, registra pagos, reporta problemas. *Rol definido y reservado en este spec (constante, presente en el modelo de roles), pero el alta de cuentas de Inquilino y sus pantallas propias quedan fuera de este spec — son funcionalidad nueva, no seguridad.*

**Actores secundarios (fuera de alcance, futuro):**
- Agente/Corredor — intermedia entre propietario e inquilino.
- Contador/Financiero — revisa reportes de pagos, genera estados de cuenta.
- Técnico/Mantenimiento — recibe y gestiona solicitudes de reparación.

**Actores externos (fuera de alcance, futuro):**
- Pasarela de pagos (PayPal, Stripe, etc.).
- Servicio de email/SMS para notificaciones.
- SRI / sistema tributario (facturación electrónica, contexto Ecuador).
- Banco (confirmación de transferencias).

No se diseña ni se implementa nada de los actores secundarios/externos en este spec; quedan documentados aquí para que el día que se aborden no se pierda el contexto de por qué existen como concepto.

## 1. Modelo de datos y cuentas

### Entidad `Usuario` (nueva, `RentaFacil.API/Models/Usuario.cs`)
- `Id` (int, PK)
- `NombreUsuario` (string, único)
- `Email` (string?)
- `PasswordHash` (string, BCrypt)
- `Rol` (string — uno de `AppRoles`)
- `Activo` (bool)
- `FechaCreacion` (DateTime)

### Constantes de rol (`RentaFacil.Shared/AppRoles.cs`)
```csharp
public static class AppRoles
{
    public const string Administrador = "Administrador";
    public const string Propietario = "Propietario";
    public const string Inquilino = "Inquilino"; // reservado, sin flujo propio todavía
}
```

### Siembra (reemplaza el seed dummy actual de `Program.cs`)
- Al migrar, si la tabla `Usuarios` está vacía, se crea **un usuario dueño** con rol `Administrador`. Usuario/contraseña inicial se leen de configuración (User Secrets en desarrollo, variables de entorno en producción) — **nunca hardcodeados** en el código fuente.
- Los datos dummy existentes (`UsuarioId = 1`) se remapean al `Id` real del usuario sembrado **sin asumir que sea `1`** (el autoincrement de SQLite no lo garantiza): `UPDATE Inquilinos/Inmuebles/... SET UsuarioId = (SELECT Id FROM Usuarios WHERE Rol = 'Administrador' LIMIT 1)`, ejecutado dentro de la misma migración EF Core que crea la tabla `Usuarios` (vía `migrationBuilder.Sql(...)`), no asumido en código C# aparte.
- El seed de datos de ejemplo (Inquilino/Inmueble/Unidad/Contrato/Pago dummy, en bases nuevas sin datos previos) se mantiene igual, pero asignando `UsuarioId = adminUser.Id` leído del objeto recién insertado en memoria (no un literal `1`).

### Propagar `UsuarioId` a las 5 entidades
Hoy solo `Inquilino` e `Inmueble` tienen `UsuarioId`; `Unidad`, `Contrato` y `Pago` no lo tienen (cuelgan de su padre). Se agrega `UsuarioId` (FK a `Usuario`) también a `Unidad`, `Contrato` y `Pago`, sellado automáticamente al crear (tomado del usuario autenticado, no del DTO del cliente).

**Por qué desnormalizar en vez de resolver `UsuarioId` vía join al padre:** permite que el filtro de propiedad sea uniforme en todas las consultas (`Where(x => x.UsuarioId == usuarioActual)`) sin tener que atravesar relaciones distintas por entidad, y es más difícil que un Repository nuevo se olvide de filtrar.

### Filtro IDOR
Cada método de `Repository` que lista o busca por id (`GetAllAsync`, `GetByIdAsync`, y los `Update`/`Delete` que cargan la entidad antes de modificarla) recibe el `UsuarioId` del usuario autenticado y filtra en el `Where` — **el filtro vive en el Repository, no en el Controller ni en el Service**, para que ningún endpoint nuevo lo omita por accidente. El `UsuarioId` se obtiene del `ClaimsPrincipal` (claim `sub` del JWT) vía `IHttpContextAccessor`, nunca del body del request.

## 2. Autenticación de servidor (JWT)

- **`POST /api/auth/login`** (`[AllowAnonymous]`): recibe `NombreUsuario`/`Password`, valida con `BCrypt.Verify`, devuelve un JWT con claims `sub` (UsuarioId), `rol`, expiración fija de 8 horas (mismo valor que usa el proyecto hermano CampeonatoATP; no es deslizante porque un JWT no se puede renovar a mitad de vida sin un endpoint de refresh, que queda fuera de alcance — al expirar, el cliente vuelve a `Login.razor`).
- **`POST /api/auth/registrar`**: solo accesible para rol `Administrador` (`[Authorize(Roles = AppRoles.Administrador)]`) — alta de nuevos usuarios (Propietario/Administrador). No hay registro público.
- **`Program.cs`**: `AddAuthentication().AddJwtBearer(...)` + `AddAuthorization` con una fallback policy que exige usuario autenticado por defecto en todos los Controllers; `[AllowAnonymous]` explícito solo en `auth/login`. Clave de firma del JWT desde configuración (User Secrets / variable de entorno), nunca en el código.
- No se modifica la decisión existente de `UseHttpsRedirection()` comentado ni la política CORS abierta — siguen vigentes para Fase 1 (LAN).

## 3. Cliente MAUI

- `AuthService.cs` deja de validar contra `Preferences`. Pasa a llamar `POST /api/auth/login` vía `ApiClient`, y si es exitoso guarda el JWT en `SecureStorage` (no en `Preferences`, que no está cifrado).
- `ApiClient.cs` adjunta `Authorization: Bearer <token>` a cada request vía un **`DelegatingHandler`** registrado en el `HttpClient` (no header por defecto): centraliza en un solo lugar tanto la lectura del token desde `SecureStorage` por request (cubre el caso de token ausente) como la detección de `401` para disparar el logout, en vez de duplicar esa lógica en cada llamada de `ApiClient`.
- `Login.razor` se ajusta para mostrar el error real que devuelva la API (credenciales inválidas) en vez de la validación local actual. `Logout` borra el token de `SecureStorage`.

## 4. Auditoría de cambios

- **`IAuditable`** (`RentaFacil.API/Models/IAuditable.cs`):
  ```csharp
  public interface IAuditable
  {
      int? CreadoPorId { get; set; }
      DateTime? FechaCreacion { get; set; }
      int? ModificadoPorId { get; set; }
      DateTime? FechaModificacion { get; set; }
  }
  ```
  `int?` para coincidir con el tipo real de `Usuario.Id`/`UsuarioId` en todo el proyecto (todas las PK/FK del repo son `int`, no `long`).
  Implementado por `Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`.
- **`AuditoriaInterceptor`** (`SaveChangesInterceptor`), registrado vía `.AddInterceptors(...)` en `AddDbContext`. En `EntityState.Added` sella `CreadoPorId`/`FechaCreacion` y `ModificadoPorId`/`FechaModificacion` con el mismo valor; en `EntityState.Modified` solo actualiza `ModificadoPorId`/`FechaModificacion`. El usuario actual se lee del `ClaimsPrincipal` vía `IHttpContextAccessor` (ya que hay auth real, no hace falta confiar en el DTO del cliente).
- **Caso sin `HttpContext` (seed/migración):** el seed de `Program.cs` corre en un scope al arrancar la app, sin ninguna request HTTP activa — `IHttpContextAccessor.HttpContext` será `null` ahí. El interceptor debe comprobar la nulidad y, si no hay contexto HTTP (o no hay claim `sub`), dejar los campos de auditoría en `null` en vez de lanzar excepción.
- Auditoría es complementaria al filtro IDOR (sección 1): una cosa es *quién hizo qué* (auditoría), otra es *evitar que alguien lea/edite datos de otro* (IDOR). No se sustituyen.
- Migración EF Core que agrega las 4 columnas de auditoría a las 5 tablas (además de las columnas `UsuarioId` nuevas en `Unidad`/`Contrato`/`Pago` y la tabla `Usuarios`).

## 5. Cabeceras de seguridad HTTP

Middleware en `Program.cs`, antes de `app.MapControllers()`:
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
El CSP se ajusta al único CDN real que usa el cliente (`cdn.jsdelivr.net` para `bootstrap-icons`, visto en `RentaFacil.MAUI/wwwroot/index.html`) — no se copia la lista de CDNs del proyecto hermano CampeonatoATP sin revisar. No depende de `UseHttpsRedirection()`, que sigue comentado a propósito.

**Por qué esto no afecta al cliente MAUI:** estas cabeceras viven en `RentaFacil.API` y solo se adjuntan a las respuestas HTTP de la API. El cliente es **Blazor Hybrid** (no Blazor WASM): `BlazorWebView` renderiza el contenido embebido en `RentaFacil.MAUI/wwwroot/` localmente dentro de la app nativa (carga `_framework/blazor.webview.js`, no `blazor.webassembly.js`) y nunca navega contra la API vía HTTP — `ApiClient` solo hace llamadas JSON. Esta CSP nunca llega al WebView del cliente.

El único HTML real que sirve la API es Swagger UI (solo en `Development`). Como el middleware de CSP se registra **después** del bloque `UseSwagger()/UseSwaggerUI()` existente, y Swagger UI intercepta sus propias rutas sin propagar la ejecución hacia abajo en el pipeline, las respuestas de Swagger UI no deberían recibir esta cabecera. Aun así, **antes de mergear esta sección, verificar manualmente con el navegador en `/swagger` (Development) que no aparezcan errores de CSP en la consola** — es la única superficie HTML real donde podría importar, y es barato de comprobar.

## 6. Rate limiting en login

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
`app.UseRateLimiter()` en el pipeline; `[EnableRateLimiting("auth")]` solo en el endpoint `POST /api/auth/login`.

## 7. Validación de identificación (Ecuador)

`ValidationAttribute` reutilizable en `RentaFacil.Shared/Validaciones/IdentificacionEcuatorianaAttribute.cs` (vive junto a los DTOs porque se aplica sobre `CrearInquilinoDto.Identificacion`, que es donde corre la validación de `[ApiController]` automáticamente; ponerlo en `Shared` también permite reusarlo si en el futuro el cliente MAUI quiere validar en el formulario antes de enviar):
- 10 dígitos → valida checksum módulo 10 de cédula ecuatoriana de persona natural.
- 13 dígitos → valida RUC: cédula + sufijo `001` (persona natural) o algoritmo de RUC de sociedades (según el tercer dígito).
- Cualquier otro formato → inválido. No contempla pasaporte (no confirmado que aplique a los inquilinos actuales).

## 8. Testing

Siguiendo el patrón existente de `RentaFacil.Tests` (Service con Repository mockeado vía Moq + FluentAssertions):
- **Auth:** hash/verify de contraseña con BCrypt; generación correcta de claims del JWT (sub, rol).
- **IDOR cerrado:** por cada Service (`InquilinoService`, `InmuebleService`, `ContratoService`, `PagoService`, `UnidadService` si se crea), test de que un usuario no puede leer/editar/borrar una entidad que pertenece a otro `UsuarioId` (debe devolver `null`/`NotFound`, no la entidad ajena).
- **Auditoría:** test de que `Added` sella `CreadoPorId`/`FechaCreacion` y que `Modified` solo actualiza `ModificadoPorId`/`FechaModificacion` sin tocar los campos de creación; test de que `AuditoriaInterceptor` **no lanza excepción** cuando `IHttpContextAccessor.HttpContext` es `null` (caso seed/migración) y deja los campos de auditoría en `null` en vez de romper.
- **Validación de identificación:** casos válidos/inválidos conocidos de cédula (10 dígitos) y RUC (13 dígitos), incluyendo el caso de checksum incorrecto.

## Fuera de alcance (explícitamente)

- Pantallas, alta de cuentas o flujo propio para el rol `Inquilino` (queda reservado como constante de rol, sin funcionalidad).
- Roles/actores secundarios y externos del modelo completo (Agente, Contador, Técnico, pasarela de pagos, email/SMS, SRI, Banco).
- ASP.NET Identity, OAuth con Google, multiusuario más allá del Administrador/Propietario sembrado.
- Pruebas de carga k6 (quedan para cuando se aborde escalar usuarios, ver `ClaudeCampeonatoatp.md`).
- Revisión de UX/UI (spec separado).
