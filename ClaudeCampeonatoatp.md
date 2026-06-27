# ClaudeCampeonatoatp.md

Extracto de `CampeonatoATP/CLAUDE.md` (proyecto hermano, mismo autor, ASP.NET Core MVC + .NET 10 + EF Core) con todo lo **usable e implementable** en RentaFacil. No es para copiar tal cual: RentaFacil es **MAUI Blazor Hybrid** (móvil + escritorio + web, vistas `.razor` en `Components/Pages/`), no MVC con `.cshtml` como Campeonato. Cada punto dice si aplica directo, si hay que adaptarlo a Blazor/móvil, o si no aplica y por qué.

Sirve como base de trabajo para la sección "Pendiente" de `CLAUDE.md` — aquí está el detalle completo; `CLAUDE.md` solo apunta a este archivo.

---

## 1. Reglas de proceso para CLAUDE.md (✅ aplica igual, no es tech-específico)

CampeonatoATP exige 4 reglas que son puro proceso, no arquitectura — se pueden adoptar en RentaFacil tal cual:

1. Actualizar `CLAUDE.md` tras cada cambio mediano/mayor, en una sección **"Último Cambio, Contexto"** que se **reescribe** (no se acumula historial largo).
2. Revisar/actualizar `CLAUDE.md` después de cada `git pull`, para no perder contexto.
3. Mantener solo un breve registro cronológico al final si aporta (no un changelog completo — eso es trabajo de `git log`).
4. Responder siempre en español — esto ya aplica en RentaFacil por convención de idioma del código.

**✅ Ya RESUELTO (2026-06-26):** `CLAUDE.md` tiene la sección "Último Contexto" (equivalente a "Último Cambio, Contexto" de Campeonato), que se reescribe en cada checkpoint — no acumula histórico. Además se agregó una regla explícita de verificar `docs/contexto/` completo al cerrar tareas (ver sección "Regla: verificar `docs/contexto/`..." en `CLAUDE.md`), nacida justo de un caso real donde estos mismos documentos quedaron desactualizados tras implementar seguridad.

---

## 2. Autenticación real con roles — ✅ ya RESUELTO (2026-06-26)

**Implementado en `feature/seguridad-auditoria` (mergeada a `main`):** `AutenticacionService` con BCrypt para hash de contraseñas (ya no hay texto plano en `Preferences`), JWT en vez de cookies (la elección difiere de Campeonato por ser un cliente MAUI/API REST, no MVC con sesión de navegador — `AuthHeaderHandler` en MAUI persiste y reenvía el token), roles vía `RentaFacil.Shared/AppRoles.cs` (`Administrador`/`Propietario`, análogos a los de Campeonato pero sin sobre-diseñar combos tipo `Gestores` hasta que haga falta), y rate limiting en `/api/auth/login`.

**Diferencia consciente con Campeonato:** no se portó el patrón "primer usuario registrado = Administrador automático" — RentaFacil siembra el admin desde `SeedAdmin:Usuario`/`SeedAdmin:Password` (user-secrets) en `Program.cs`, y el registro de cuentas nuevas (`POST /api/auth/registrar`) requiere ya estar autenticado (no es self-service público). Revisar si se necesita registro público cuando se aborde multiusuario real (Fase 2/3).

---

## 3. Seguridad HTTP — cabeceras y rate limiting — ✅ ya RESUELTO (2026-06-26)

Esto es independiente de MVC vs Blazor: vive en el pipeline de `Program.cs` de la Web API, que en ambos proyectos es ASP.NET Core puro. Código de referencia de Campeonato:

```csharp
// Cabeceras de seguridad HTTP
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] = "default-src 'self'; ...";
    await next();
});

// Rate limiting (anti fuerza bruta en endpoints de auth)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, QueueLimit = 0 }));
});
// ...
app.UseRateLimiter();
```

**Aplicar en RentaFacil tal cual**, con dos matices:

Implementado tal cual en `RentaFacil.API/Program.cs`: cabeceras (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy` ajustado a los CDNs reales de RentaFacil, no copiado literal de Campeonato) + rate limiting política `"auth"` (10/min por IP) en `/api/auth/login`. `UseHttpsRedirection()` sigue desactivado a propósito (LAN), las cabeceras no dependen de eso.

---

## 4. Auditoría automática de cambios — ✅ ya RESUELTO (2026-06-26)

Patrón de Campeonato, 100% portable porque es EF Core + C# sin nada de MVC/Blazor:

```csharp
public interface IAuditable
{
    long? CreadoPorId { get; set; }
    string? CreadoPor { get; set; }
    DateTime? FechaCreacion { get; set; }
    long? ModificadoPorId { get; set; }
    string? ModificadoPor { get; set; }
    DateTime? FechaModificacion { get; set; }
}

public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IUsuarioActualService _usuarioActual;
    // override SavingChanges/SavingChangesAsync → recorre context.ChangeTracker.Entries<IAuditable>()
    // Added → sella Creado* y Modificado* con el mismo valor; Modified → solo actualiza Modificado*.
}
```

Registrado en `AddDbContext` vía `.AddInterceptors(...)`.

**Implementado en RentaFacil:** `IAuditable` (`CreadoPorId`/`FechaCreacion`/`ModificadoPorId`/`FechaModificacion`, `int?` en vez de `long?` porque los Id de RentaFacil son `int`) en las 5 entidades + `AuditoriaInterceptor` registrado en `AddDbContext`. A diferencia de Campeonato (que ya tenía sesión HTTP con cookie), el interceptor de RentaFacil lee el usuario del `ClaimsPrincipal` vía `IHttpContextAccessor` — esto solo fue posible *después* de implementar JWT real (punto 2); mientras no había auth de servidor, no había `HttpContext.User` confiable de donde leer. El interceptor maneja el caso `HttpContext == null` (el seed de datos corre sin request HTTP activa) sin lanzar excepción.

Sigue siendo cierto lo que ya decía esta sección: auditoría es complementaria al IDOR (punto 5) — dice *quién hizo qué*, no evita que un usuario lea/edite datos de otro. Ambos ya están resueltos, pero son arreglos distintos.

---

## 5. IDOR/BOLA — filtrar por `UsuarioId` — ✅ ya RESUELTO (2026-06-26, era 🔴 el hallazgo #1)

Campeonato hizo una auditoría IDOR/BOLA dedicada (Proyecto A) y **no encontró hallazgos**, porque sus controllers/repos sí filtraban por el usuario autenticado en cada query. RentaFacil **no pasaba esa misma auditoría** hasta esta fecha: se confirmó leyendo el código que ningún `GetAllAsync`/`GetByIdAsync` de `InquilinoService`/`InmuebleService`/`OtherServices.cs` filtraba por `UsuarioId`.

**Resuelto:** `UsuarioId` se agregó como parámetro a todos los métodos de Repository que listan o buscan por id, filtrando el `Where` ahí (no en el Controller) en las 5 entidades + el recibo PDF. Verificado repetidamente con un segundo usuario real que no ve, lee, ni crea sobre ningún dato del primero.

---

## 6. Validación de identificación ecuatoriana — ✅ ya RESUELTO (2026-06-26)

Campeonato tiene `CedulaEcuatorianaAttribute`, que solo valida cédula de persona natural (10 dígitos, módulo 10). RentaFacil necesitaba más que eso porque `Inquilino.Identificacion` también acepta RUC (13 dígitos) — se implementó `IdentificacionEcuatorianaAttribute` en `RentaFacil.Shared/Validaciones/` con la rama adicional que Campeonato no tenía: módulo 10 para cédula/RUC de persona natural, módulo 11 para RUC de sociedad.

**Gotcha real encontrado (no estaba en el análisis de Campeonato):** en un `record` de C#, el atributo de validación debe ir directo sobre el parámetro del constructor (`[IdentificacionEcuatoriana] string Identificacion`), nunca con `[property: ...]` — esa segunda forma compila y pasa tests unitarios pero revienta en runtime con `InvalidOperationException` en la validación de ASP.NET Core sobre records (devuelve `500` en vez de `400`/`201`). Solo se detectó probando con curl contra la API corriendo.

---

## 7. Pruebas de carga con k6 (✅ aplica igual, es HTTP puro)

Campeonato tiene `loadtests/load-test.js` (rampa hasta 20 VUs sobre `/`, `/Auth/Login`, `/Partidos`, con thresholds de p95 < 800ms y error < 1%) y `stress-test.js` (50→100→200 VUs). k6 prueba HTTP, no le importa si el backend es MVC o Web API pura — aplica igual a `RentaFacil.API`.

Diferencia a tener en cuenta: los endpoints de RentaFacil a probar serían `/api/inquilinos`, `/api/inmuebles`, `/api/contratos`, `/api/pagos` (JSON, no HTML), y los `GetAll()` de RentaFacil hoy no paginan — con pocos datos no importa, pero es la primera cosa que un stress test va a exponer si la base de datos crece. Vale la pena correr esto **antes** de escalar a más usuarios, no después.

---

## 8. Patrones de UI — qué adaptar a Blazor `.razor` y qué no aplica

Esta es la parte donde Campeonato (MVC + `.cshtml`) y RentaFacil (Blazor Hybrid + `.razor`) divergen más, porque son paradigmas de UI distintos:

- **❌ No aplica: convención de nombres `<Acción><Controlador>.cshtml`** (`IndexEquipos.cshtml`, `CreatePartidos.cshtml`). RentaFacil ya tiene su propia convención en `Components/Pages/*.razor` (`Inquilinos.razor`, `CrearInquilino.razor`, `DetallePagos.razor`) — **mantenerla**, no migrar a estilo Campeonato. El usuario lo pidió explícito: "vamos a mantener el estándar de las páginas que ya tenemos".
- **❌ No aplica: `_LayoutPartidos.cshtml` por área**. Blazor Hybrid ya resuelve layouts distinto vía `Components/Layout/MainLayout.razor`/`LoginLayout.razor` + `@layout` en cada página o en `Routes.razor`. No hay equivalente "por área" que portar.
- **🔄 Adaptar (no copiar literal): modales → menús contextuales.** Campeonato reemplazó modales por fila por menús desplegables contextuales en `Clasificacion`/`Rankings`, porque su público es desktop/web. RentaFacil es **móvil + escritorio + web** (Blazor Hybrid corre en Android/iOS/Windows/Mac, y a futuro Blazor Server/WASM para dashboard web — ver Fase 3 del plan). El patrón correcto para RentaFacil no es "copiar menú contextual", es **responsive**: las páginas de listado (`Inquilinos.razor`, `Inmuebles.razor`) ya usan **bottom sheet**, que es el patrón móvil correcto y NO debe reemplazarse por menú contextual ahí. Si en el futuro se agrega una vista de escritorio/web más densa (tabla de Contratos o Estado de Pagos en pantalla grande), ahí sí evaluar un menú contextual tipo Campeonato como complemento — no como sustituto del bottom sheet en móvil.
- **🔄 Adaptar: DTOs `record` chicos para endpoints puntuales.** Campeonato declara `record` DTOs al final del mismo archivo de controller (`PartidosController.cs`) para payloads AJAX chicos, en vez de crear un ViewModel nuevo por cada uno. El equivalente en RentaFacil/Blazor: cuando un endpoint de la API es puntual y chico, seguir poniendo el `record` DTO en `RentaFacil.Shared/Models/*Dto.cs` (ya es la convención existente) en vez de inventar una carpeta nueva — el principio ("no crear un archivo nuevo por cada DTO chico") es el mismo, el lugar cambia porque RentaFacil ya centraliza DTOs en `Shared`.
- **❌ No aplica: separar `Models/ViewModels` de nivel raíz solo para auth, vs ViewModels de dominio en la librería Core.** RentaFacil no tiene la separación en dos proyectos (`Core` vs Web) que tiene Campeonato — ver punto 9. No hay "ViewModels de dominio" que mover, ya están en `Shared`.

---

## 9. Cosas que explícitamente NO aplican (y por qué)

Para que quede registrado y no se reconsidere sin motivo:

- **División en dos proyectos (`*.Core` class library + proyecto web)**: Campeonato separa `CampeonatoATP.Core` (Models/Data/Repositories/Services) del proyecto MVC. RentaFacil ya tiene una separación equivalente pero distinta: `RentaFacil.Shared` (solo DTOs/Enums) + `RentaFacil.API` (todo lo demás: Models, Data, Repositories, Services, Controllers). Es un estándar ya establecido — no migrar a la estructura de Campeonato.
- **Repository genérico (`IGenericRepository<T>`)**: Campeonato usa un repo genérico abierto para todas las entidades. RentaFacil usa un repo+interfaz por entidad (`IInquilinoRepository`, `IContratoRepository`, etc.) — son ~5 entidades, no justifica la abstracción genérica todavía. Mantener el estándar actual.
- **Esquemas de base de datos — corregido, esto SÍ se adoptó (2026-06-26):** esta entrada decía que RentaFacil usaba SQLite/MySQL y que los esquemas de Campeonato (`Torneo`/`Inscripciones`/`Seguridad`) no aplicarían. Quedó obsoleta: RentaFacil migró a **SQL Server** (ver `docs/contexto/decisiones.md`) y adoptó 4 schemas propios — `auth`/`renta`/`config`/`audit` (no calcados de los de Campeonato, pero el mismo patrón de organizar por schema en vez de un solo `dbo`). Detalle en `docs/superpowers/plans/2026-06-26-migracion-sqlserver.md`.
- **Seeding con `HasData` de catálogos estáticos**: Campeonato precarga 45 categorías deportivas fijas vía `HasData`. RentaFacil no tiene catálogos estáticos de ese tipo — sus datos (`Inquilinos`, `Inmuebles`...) son datos de usuario, no catálogo. El seed actual de `Program.cs` (datos dummy si la tabla está vacía) ya cubre el caso de uso real de RentaFacil (probar contra una BD limpia).
- **`PartidosController` como mega-controller con DTOs `record` al final**: el tamaño (~1000 líneas) es consecuencia del dominio de Campeonato (partidos con árbitros, eventos, comentarios, bracket). No hay un controller de RentaFacil con esa complejidad ni se espera que la tenga.

---

## Resumen — orden de prioridad sugerido si se implementa algo de esto

**Estado (2026-06-26): puntos 1-5 ya implementados y mergeados a `main`** (rama `feature/seguridad-auditoria`). Solo queda pendiente el punto 6.

1. ~~Filtrar por `UsuarioId` en repos/services (punto 5)~~ — ✅ resuelto.
2. ~~Auditoría (`IAuditable` + interceptor) (punto 4)~~ — ✅ resuelto.
3. ~~Cabeceras de seguridad HTTP (punto 3)~~ — ✅ resuelto.
4. ~~Validación de cédula/RUC (punto 6 de la lista numerada arriba)~~ — ✅ resuelto.
5. ~~Auth real + roles + rate limiting de login (punto 2)~~ — ✅ resuelto.
6. **k6** (punto 7) — único pendiente. Útil como gate antes de escalar usuarios, no urgente con un solo usuario hoy.
