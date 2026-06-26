# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio.

> **Este archivo es solo un índice.** Da mini-resúmenes y enlaza al detalle. NO leas todo: lee la sección que necesites y, si requieres más, abre el `.md` enlazado. Responder siempre en español.

## En una frase
RentaFácil: app personal (aún de un solo usuario) para que un arrendador gestione inquilinos, inmuebles/unidades, contratos de alquiler y pagos, con recibos en PDF. Cliente **.NET MAUI Blazor Hybrid** (móvil + escritorio + web, páginas `.razor`) + backend **ASP.NET Core Web API** (.NET 10, EF Core, SQLite local → MySQL en producción). Código, comentarios y UI en español.

## Arranque rápido
- Solución: `RentaFacil.slnx` (no `.sln`). 4 proyectos: `RentaFacil.Shared` (DTOs/enums), `RentaFacil.API` (backend), `RentaFacil.MAUI` (cliente), `RentaFacil.Tests` (xUnit+Moq+FluentAssertions).
- Build: `dotnet build RentaFacil.slnx` · API: `dotnet run --project RentaFacil.API` (escucha en `http://0.0.0.0:5295`) · Tests: `dotnet test RentaFacil.Tests`.
- Test único: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~InquilinoServiceTests.CrearAsync_ShouldReturnCreatedInquilinoDto"`.
- Migraciones (desde `RentaFacil.API/`): `dotnet ef migrations add <Nombre>` / `dotnet ef database update`. Se aplican solas al arrancar la API.
- MAUI: algunos TFM solo compilan en su SO (iOS/MacCatalyst en macOS, `windows` en Windows). Android en Windows: `dotnet build RentaFacil.MAUI -f net10.0-android`.

## Contexto del proyecto
Cada eje en su archivo — abre solo el que necesites:

- **Arquitectura** → @docs/contexto/arquitectura.md — stack, mapa de carpetas, flujo de datos (Blazor → `ApiClient` → Controller → Service → Repository → EF Core), esquema de BD y reglas de borrado, y "lo que NO existe".
- **Convenciones** → @docs/contexto/convenciones.md — idioma español, naming, DTOs `record` en `Shared`, capas `Model→Repository→Service→Controller`, páginas `.razor` (no `.cshtml`), bottom sheet en móvil, tests de Services con repo mockeado.
- **Decisiones** → @docs/contexto/decisiones.md — SQLite/MySQL, `LOCAL` compile constant para la URL de la API, CORS abierto + HTTPS off (LAN), login local temporal, migración automática, versionado SemVer + respaldo por APK.
- **Glosario** → @docs/contexto/glosario.md — términos del dominio (Inmueble Único/Múltiple, Unidad, Contrato, Pago, Periodo, Facturado/Completado), entidades con sus campos, indicadores de color del Estado de Pagos, siglas internas.
- **Flujo de trabajo** → @docs/contexto/flujo-de-trabajo.md — pasos para un cambio, checklist de "terminado", y las 3 fases de deploy (Local actual / Render / Oracle Cloud).
- **Errores conocidos** → @docs/contexto/errores-conocidos.md — IDOR por falta de filtro `UsuarioId`, login que no protege la API, `UnidadesController` salta capas, IP de prod hardcodeada, `rentafacil.db` versionado, y cosas que parecen rotas pero son a propósito (CORS/HTTPS, seed dummy).

## Pendiente
Lista de lo que falta implementar. El análisis de seguridad/auditoría a fondo (con código de referencia del proyecto hermano CampeonatoATP) vive en → @ClaudeCampeonatoatp.md.

**Seguridad/auditoría (prioridad, ver ClaudeCampeonatoatp.md para el detalle y orden):**
1. 🔴 **Filtrar por `UsuarioId`** en repos/services — hoy cualquiera lee/edita datos de cualquier usuario (IDOR/BOLA confirmado). Es lo más urgente.
2. Auditoría de cambios (`IAuditable` + `SaveChangesInterceptor`): quién/cuándo creó/modificó cada fila.
3. Cabeceras de seguridad HTTP en `Program.cs` (X-Frame-Options, CSP, etc.).
4. Validación de cédula/identificación (atributo reutilizable para `Inquilino.Identificacion`).
5. Autenticación real de servidor (BCrypt + roles + rate limiting de login) — solo cuando haga falta multiusuario.
6. Pruebas de carga k6 antes de escalar usuarios.

**Funcionalidad (Fase 2 / Fase 3, del plan de producto):**
- Multiusuario real: ASP.NET Identity + JWT, login con Google (OAuth 2.0).
- Migrar SQLite → MySQL; dockerizar API + BD; deploy en Render → Oracle Cloud.
- Notificaciones de vencimiento de pago; compartir recibo por WhatsApp (deep link).
- Medidores de servicios (agua/luz) por unidad; módulo Ingresos con gráficas.
- Confirmar/implementar semántica de color del Estado de Pagos (ver glosario).
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web, firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-06-26
**Plan en ejecución:** `docs/superpowers/plans/2026-06-26-seguridad-auditoria.md` (20 tasks), spec en `docs/superpowers/specs/2026-06-26-seguridad-auditoria-design.md`, ejecución inline (no subagentes) en la rama `feature/seguridad-auditoria` (creada desde `main`, no mergeada).

**Tasks 1-14 COMPLETAS y commiteadas** (70%) — autenticación, auditoría, e IDOR cerrado en Inquilino/Inmueble/Unidad, todo verificado end-to-end con un segundo usuario real (registrado vía `/api/auth/registrar`) que no puede ver ni crear sobre datos del admin. Commits en orden en `feature/seguridad-auditoria`:
1. Paquetes BCrypt+JwtBearer.
2. Entidad `Usuario`+`AppRoles`+`IUsuarioRepository` (migración `AddUsuarios`).
3. DTOs `LoginDto`/`LoginResultDto`/`RegistrarUsuarioDto`.
4. `AutenticacionService` (BCrypt+JWT, 5 tests verdes).
5. `AuthController` + wiring JWT Bearer + `AddAuthorization` con `FallbackPolicy = RequireAuthenticatedUser()`.
6. Cabeceras de seguridad HTTP (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`). **Nota:** un "bug de Swagger bloqueado por 401" que parecía existir era un falso positivo de testing — `curl -I` envía `HEAD` (Swagger no lo maneja, cae al fallback auth); con GET real (`curl -i`) Swagger responde `200` sin token. No hace falta `UseWhen` ni excepción especial para `/swagger`. **Si algo "parece" bloqueado, probar primero con GET real antes de tocar el pipeline de auth.**
7. Rate limiting en `/api/auth/login` (política `"auth"`, 10/min por IP, verificado: intento 11 → `429`).
8. `ClaimsPrincipalExtensions.ObtenerUsuarioId()` (2 tests verdes).
9. Siembra del usuario dueño desde `SeedAdmin:Usuario`/`SeedAdmin:Password` (user-secrets) + remapeo de `Inquilino`/`Inmueble` existentes al `Id` real del admin (sin asumir `1`). Verificado con base limpia: login real con `duenotest`/`CambiaEstaClave123!` (estas son credenciales de **desarrollo local**, viven solo en user-secrets de esta máquina, no en el repo).
10. `IAuditable` (`int?` no `long?`) implementado en las 5 entidades + migración `AddAuditoriaColumns`. 14 tests verdes en total.
11. `AuditoriaInterceptor` (`SaveChangesInterceptor`) registrado en `AddDbContext` vía el overload `(sp, options) => ...AddInterceptors(...)`. 3 tests verdes con `SqliteConnection(":memory:")`, incluyendo el caso `HttpContext == null` (el seed de la Task 9 corre sin request HTTP activa). Verificado manualmente: la API arranca limpio con base nueva, sin excepciones.
12. IDOR cerrado en Inquilino: `CrearInquilinoDto` ya no recibe `UsuarioId` del cliente, todo el filtrado vive en `InquilinoRepository`. Nota de implementación: este cambio de firma rompió `ReciboService.GenerarReciboPdfAsync` (llamaba a `IInquilinoRepository.GetByIdAsync` sin `usuarioId`) — se le agregó el parámetro `usuarioId` ahí mismo como fix de compilación mínimo; el cierre completo del IDOR de recibos (Pago/Contrato) es la Task 16.
13. IDOR cerrado en Inmueble, mismo patrón.
14. `Unidad` ganó su propio `IUnidadRepository`/`UnidadService` (ya no usa `AppDbContext` directo en el controller — cerraba el anti-patrón de `errores-conocidos.md`), `UsuarioId` propio, y `CrearAsync` valida que el `InmuebleId` del dto pertenezca al usuario autenticado (si no, `400`). Migración `AddUsuarioIdToUnidad` (con `defaultValue: 0` + remapeo en el seed vía `Include(u => u.Inmueble)`).

22 tests verdes en total. Verificación manual end-to-end repetida en cada task: usuario `otro` (`Propietario`, registrado vía `/api/auth/registrar`) ve listas vacías y `404`/`400` al intentar leer o crear sobre datos del admin (`duenotest`).

Commit del `UserSecretsId` en el `.csproj` (necesario para que `dotnet user-secrets` funcione) también hecho (`ff0157c`).

**Pendiente de hacer en cada checkpoint (pedido explícito del usuario):** reescribir esta sección tras cada checkpoint de tasks completadas, no solo al final.

**Detalle exacto de lo que falta por task (15-20), para retomar sin releer el plan completo:**

- **Task 15 — IDOR en Contrato.** Modify `Contrato.cs`: agregar `UsuarioId`. Modify `IOtherRepositories.cs`/`OtherRepositories.cs` (`ContratoRepository`): filtrado. Modify `IOtherServices.cs`/`OtherServices.cs` (`ContratoService`): constructor gana `IInquilinoRepository`+`IUnidadRepository` para validar que `InquilinoId`/`UnidadId` del dto pertenezcan al `usuarioId`; `CrearAsync`/`UpdateAsync` devuelven `null`/`false` si no. Modify `OtherControllers.cs` (`ContratosController`): `User.ObtenerUsuarioId()` + manejar `null`/`false` con `BadRequest`/`NotFound`. Migración `AddUsuarioIdToContrato`. Extender seed: `Contrato.UsuarioId = Inquilino.UsuarioId` de su padre. Modify `OtherServiceTests.cs` (`ContratoServiceTests`).

- **Task 16 — IDOR en Pago + recibo PDF.** Modify `Pago.cs`: agregar `UsuarioId`. Modify `IOtherRepositories.cs`/`OtherRepositories.cs` (`PagoRepository`): filtrado. Modify `IOtherServices.cs`/`OtherServices.cs` (`PagoService`): constructor gana `IContratoRepository` para validar `ContratoId`. Modify `ReciboService.cs`: `IReciboService.GenerarReciboPdfAsync` gana parámetro `usuarioId`, lo pasa a los 3 repos que consulta (Pago/Contrato/Inquilino). Modify `OtherControllers.cs` (`PagosController`, incluye `GetRecibo`): `User.ObtenerUsuarioId()`. Migración `AddUsuarioIdToPago`. Extender seed: `Pago.UsuarioId = Contrato.UsuarioId` de su padre. Modify `OtherServiceTests.cs` (`PagoServiceTests`). Verificación manual end-to-end: 2do usuario no ve datos del admin.

- **Task 17 — Validación cédula/RUC.** Create `RentaFacil.Shared/Validaciones/IdentificacionEcuatorianaAttribute.cs`: 10 dígitos = cédula (módulo 10, provincia 1-24, tercer dígito 0-5); 13 dígitos = RUC (natural 0-5 mismo módulo 10 + sufijo≠`000`, o sociedad dígito `9` módulo 11 + sufijo≠`000`); todo lo demás inválido. Modify `InquilinoDto.cs`: `[property: IdentificacionEcuatoriana]` en `CrearInquilinoDto.Identificacion`. Create `RentaFacil.Tests/IdentificacionEcuatorianaAttributeTests.cs`: vectores ya derivados a mano — válidos `"1712345675"` (cédula), `"1712345675001"` (RUC natural), `"1791234561001"` (RUC sociedad); inválidos: checksum incorrecto, provincia `00`, tercer dígito `6`, sufijo `000`, letra, longitud rara, vacío.

- **Task 18 — MAUI `AuthService` + `DelegatingHandler`.** Create `RentaFacil.MAUI/Services/AuthHeaderHandler.cs`: `DelegatingHandler` que lee el token de `SecureStorage` y lo agrega como `Authorization: Bearer`, y si la respuesta es `401` llama `AuthService.Logout()`. Modify `AuthService.cs`: reescritura completa — ya NO recibe `HttpClient` por DI (lo crea él mismo en el constructor, con el mismo `#if DEBUG` cert bypass que tenía `MauiProgram`, para evitar ambigüedad de DI con el `HttpClient` con handler de `ApiClient`); gana `InicializarAsync()` (lee `SecureStorage`), `LoginAsync(usuario,password)` (llama `POST api/auth/login`, guarda token+rol en `SecureStorage`), `Logout()`; se eliminan `Register`/`GetPassword`. Modify `MauiProgram.cs`: un solo `HttpClient` (`Scoped`) envuelto en `AuthHeaderHandler` para `ApiClient`; `AuthService` `Singleton` sin depender de ningún `HttpClient` de DI. Verificación: `dotnet build RentaFacil.MAUI -f net10.0-android`.

- **Task 19 — `Login.razor` simplificado.** Modify `Login.razor`: reescritura completa — se quitan las vistas "register"/"recover"; queda solo el form de login, `await Auth.LoginAsync(...)`. Modify `MainLayout.razor`: `OnInitializedAsync` hace `await Auth.InicializarAsync()` antes de chequear `IsAuthenticated` (se quita el `OnAfterRender` viejo no-async).

- **Task 20 — Verificación final completa.** `dotnet build RentaFacil.API` (no el `.slnx` completo, por el bug preexistente de build Android en Windows, confirmado con `git stash` que no lo causamos nosotros) + `dotnet test RentaFacil.Tests` (todo en verde) + smoke test manual end-to-end (login, CRUD con cédula válida/inválida, recibo PDF, 2do usuario no ve datos del 1ro, rate limit, cabeceras) + actualizar `CLAUDE.md` sección "Pendiente" (quitar puntos ya resueltos) y "Último Contexto" final.

**Cuidado con procesos huérfanos:** durante esta sesión, `dotnet run` en background dejó el `.dll`/`.exe` bloqueados varias veces. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
