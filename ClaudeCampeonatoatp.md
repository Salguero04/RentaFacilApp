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

**Decisión pendiente:** si se adopta, agregar a `CLAUDE.md` de RentaFacil una sección "Último Cambio, Contexto" igual a la de Campeonato. Es la única adición de este punto que falta — el resto de las reglas ya se siguen informalmente.

---

## 2. Autenticación real con roles (✅ aplica cuando se implemente login de servidor)

Hoy RentaFacil no tiene autenticación de servidor (ver `docs/contexto/errores-conocidos.md`). Cuando se aborde, el patrón de Campeonato es el modelo a seguir:

- **Hash de contraseñas con BCrypt** (`AuthService` en Campeonato) — RentaFacil debe usar lo mismo en vez de guardar contraseñas en texto plano en `Preferences` (estado actual de `RentaFacil.MAUI/Services/AuthService.cs`, que además solo valida localmente y nunca llama a la API).
- **Roles vía constantes, no strings sueltos**: Campeonato define `AppRoles.Administrador/Organizador/Lector/Digitador` + un combo precalculado `AppRoles.Gestores = "Administrador,Organizador"` para el caso común, usado en `[Authorize(Roles = AppRoles.Gestores)]`. Para RentaFacil (hoy un solo arrendador, pero el plan de Fase 3 contempla "múltiples usuarios por cuenta: propietario + empleados" — ver la sección "Pendiente" de `CLAUDE.md`), un esquema análogo sería algo como `AppRoles.Propietario` / `AppRoles.Empleado`, sin sobre-diseñar hasta que haga falta.
- **Autenticación por cookies con expiración deslizante** (8h en Campeonato) configurada en `Program.cs` — aplicable igual en `RentaFacil.API/Program.cs` si el login pasa a ser server-side. Para el cliente MAUI/Blazor Hybrid esto implica que `ApiClient.cs` debe persistir y reenviar la cookie/token (hoy no envía ninguna credencial).
- **Primer usuario registrado = Administrador automático** (`AuthService.RegisterAsync` revisa si la tabla `Usuarios` está vacía): patrón directamente reutilizable el día que RentaFacil tenga registro de cuentas reales — evita tener que asignar el primer rol a mano o hardcodear un admin.

**No aplica todavía**: nada de esto debe implementarse mientras RentaFacil siga siendo de un solo usuario (Fase 1). Es para cuando se aborde Fase 2/3 del plan.

---

## 3. Seguridad HTTP — cabeceras y rate limiting (✅ aplica casi sin cambios)

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

- `RentaFacil.API/Program.cs` hoy **desactiva** `UseHttpsRedirection()` a propósito (para permitir HTTP desde el celular en LAN). Las cabeceras de seguridad no dependen de HTTPS, así que se pueden agregar igual sin tocar esa decisión.
- El CSP de Campeonato (`script-src`, `style-src`, etc.) está pensado para vistas Razor que cargan CDNs (Bootstrap Icons, jsdelivr). RentaFacil ya usa Bootstrap Icons por CDN en `RentaFacil.MAUI/wwwroot/index.html` — el CSP habría que ajustarlo a los CDNs que RentaFacil use realmente, no copiar la lista de Campeonato sin revisar.
- Rate limiting con política `"auth"` solo tiene sentido el día que exista un endpoint de login real en la API (ver punto 2). Por ahora no hay nada que limitar ahí.

---

## 4. Auditoría automática de cambios (✅ aplica directo, es puro EF Core)

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

**Para RentaFacil:**

- Hoy los modelos (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`) solo tienen `FechaRegistro`/`FechaPago` sueltos, sin `ModificadoPor*` y sin interceptor — cualquier `Update` pisa el registro sin dejar rastro de quién ni cuándo.
- Implementar `IAuditable` en las 5 entidades + un `AuditoriaInterceptor` análogo es el camino más corto a tener trazabilidad, sin tocar cada `Service` a mano (el interceptor lo hace en `SaveChangesAsync`, automático).
- Diferencia con Campeonato: ahí `IUsuarioActualService` lee el usuario de la sesión HTTP (`HttpContext`/cookie). RentaFacil no tiene sesión de servidor — `UsuarioId` hoy viaja en el DTO de cada request desde el cliente MAUI (ver `docs/contexto/errores-conocidos.md`). El interceptor puede usar ese mismo `UsuarioId` mientras no haya auth real; cuando haya login de servidor (punto 2), migrar `IUsuarioActualService` a leerlo del contexto de auth en vez de confiar en lo que mande el cliente.
- Útil notar: esto es complementario al problema de IDOR (punto 5) — auditoría dice *quién hizo qué*, pero no evita que un usuario lea/edite datos de otro. Son dos arreglos distintos, no se sustituyen.

---

## 5. IDOR/BOLA — filtrar por `UsuarioId` (🔴 más urgente que todo lo anterior, ya confirmado en este repo)

Campeonato hizo una auditoría IDOR/BOLA dedicada (Proyecto A) y **no encontró hallazgos**, porque sus controllers/repos sí filtran por el usuario autenticado en cada query. RentaFacil **no pasaría esa misma auditoría hoy**: se confirmó leyendo el código que ningún `GetAllAsync`/`GetByIdAsync` de `InquilinoService`/`InmuebleService`/`OtherServices.cs` filtra por `UsuarioId` — el campo solo se asigna al crear/editar, nunca se usa para restringir lecturas.

**Esto no es un patrón a "copiar" de Campeonato, es el hallazgo que justifica priorizar este punto antes que roles/JWT completos.** Acción concreta cuando se aborde: agregar `UsuarioId` como parámetro a los métodos de Repository/Service que listan o buscan por id, y filtrar el `Where` ahí — no en el Controller, para que ningún endpoint nuevo se olvide de hacerlo.

---

## 6. Validación de identificación ecuatoriana (✅ aplica directo a `Inquilino.Identificacion`)

Campeonato tiene `CedulaEcuatorianaAttribute`, un `ValidationAttribute` que valida el checksum módulo 10 de la cédula ecuatoriana, usado en `Jugador.Cedula`. RentaFacil tiene el mismo tipo de campo: `Inquilino.Identificacion` (DNI, Cédula de Identidad o RUC — ver `docs/contexto/glosario.md`). Si los inquilinos son mayormente ecuatorianos, vale la pena portar ese mismo atributo (mismo algoritmo, sin reescribirlo) a `RentaFacil.Shared` o `RentaFacil.API/Models`, aplicado al DTO de creación de Inquilino. Si `Identificacion` también acepta RUC (13 dígitos, empresas) o pasaporte, el atributo necesita una rama adicional que Campeonato no tiene (ahí solo valida cédula de persona natural, 10 dígitos) — no asumir que el atributo de Campeonato cubre RUC sin revisarlo.

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
- **Esquemas de base de datos (`Torneo`/`Inscripciones`/`Seguridad` en SQL Server)**: RentaFacil usa SQLite en local (sin soporte real de esquemas) y MySQL en producción (esquemas = bases de datos separadas en MySQL, no aplican igual que en SQL Server). No portable sin rediseño de `AppDbContext`.
- **Seeding con `HasData` de catálogos estáticos**: Campeonato precarga 45 categorías deportivas fijas vía `HasData`. RentaFacil no tiene catálogos estáticos de ese tipo — sus datos (`Inquilinos`, `Inmuebles`...) son datos de usuario, no catálogo. El seed actual de `Program.cs` (datos dummy si la tabla está vacía) ya cubre el caso de uso real de RentaFacil (probar contra una BD limpia).
- **`PartidosController` como mega-controller con DTOs `record` al final**: el tamaño (~1000 líneas) es consecuencia del dominio de Campeonato (partidos con árbitros, eventos, comentarios, bracket). No hay un controller de RentaFacil con esa complejidad ni se espera que la tenga.

---

## Resumen — orden de prioridad sugerido si se implementa algo de esto

1. **Filtrar por `UsuarioId`** en repos/services (punto 5) — es el único hallazgo de seguridad ya confirmado, no hipotético.
2. **Auditoría (`IAuditable` + interceptor)** (punto 4) — barato de agregar, no depende de tener auth real primero.
3. **Cabeceras de seguridad HTTP** (punto 3, mitad) — agregar el middleware de cabeceras no depende de nada más.
4. **Validación de cédula** (punto 6) — aislado, no depende de lo demás.
5. **Auth real + roles + rate limiting de login** (punto 2 + mitad del 3) — solo cuando haya necesidad real de multiusuario (Fase 2/3 del plan), no antes.
6. **k6** (punto 7) — útil como gate antes de escalar usuarios, no urgente con un solo usuario hoy.
