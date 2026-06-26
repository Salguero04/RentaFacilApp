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

**Tasks 1-17 COMPLETAS y commiteadas** (85%) — **el hallazgo de seguridad #1 original (IDOR/BOLA en las 5 entidades) está completamente cerrado**, con autenticación JWT real, auditoría automática y validación de cédula/RUC como base. Verificado end-to-end repetidamente con un segundo usuario real (`otro`/`Propietario`, registrado vía `/api/auth/registrar`) que no puede ver, leer, ni crear sobre ningún dato del admin (`duenotest`) en Inquilino, Inmueble, Unidad, Contrato, Pago, ni generar su recibo PDF. Commits en orden en `feature/seguridad-auditoria`:
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
15. IDOR cerrado en Contrato: `ContratoService` valida que `InquilinoId`/`UnidadId` del dto pertenezcan al usuario (vía los repos ya filtrados de Inquilino/Unidad) antes de crear/actualizar. Migración `AddUsuarioIdToContrato`.
16. IDOR cerrado en Pago + recibo PDF: `PagoService` valida `ContratoId`; `ReciboService.GenerarReciboPdfAsync(pagoId, usuarioId, formato)` ahora filtra los 3 repos que consulta (Pago/Contrato/Inquilino) — un usuario sin acceso al pago recibe `404` al pedir el recibo. Migración `AddUsuarioIdToPago`.
17. Validación de cédula/RUC ecuatoriano: `RentaFacil.Shared/Validaciones/IdentificacionEcuatorianaAttribute.cs` (módulo 10 para cédula/RUC natural, módulo 11 para RUC sociedad), aplicado en `CrearInquilinoDto.Identificacion`. **Gotcha real encontrado y corregido:** en un `record`, el atributo de validación debe ir directo sobre el parámetro del constructor (`[IdentificacionEcuatoriana] string Identificacion`), NO con `[property: IdentificacionEcuatoriana]` — esta segunda forma compila y pasa los tests unitarios (que llaman `_attribute.IsValid()` directo) pero revienta en runtime con `InvalidOperationException` en la validación de ASP.NET Core sobre records, devolviendo `500` en vez de `400`/`201` en cualquier request real a `POST /api/inquilinos`. Se detectó solo probando con curl contra la API corriendo, no con los tests. **Lección:** cualquier DTO `record` con atributos de validación en sus parámetros hay que verificarlo con una request HTTP real, no solo con tests unitarios del atributo.

37 tests verdes en total. Esto cierra el hallazgo 🔴 #1 de la sección "Pendiente" de este archivo (filtrar por `UsuarioId`) y el punto 4 (validación de cédula) — falta solo actualizar esa sección al terminar todo el plan (Task 20).

Commit del `UserSecretsId` en el `.csproj` (necesario para que `dotnet user-secrets` funcione) también hecho (`ff0157c`).

**Pendiente de hacer en cada checkpoint (pedido explícito del usuario):** reescribir esta sección tras cada checkpoint de tasks completadas, no solo al final.

**Detalle exacto de lo que falta por task (18-20), para retomar sin releer el plan completo:**

- **Task 18 — MAUI `AuthService` + `DelegatingHandler`.** Create `RentaFacil.MAUI/Services/AuthHeaderHandler.cs`: `DelegatingHandler` que lee el token de `SecureStorage` y lo agrega como `Authorization: Bearer`, y si la respuesta es `401` llama `AuthService.Logout()`. Modify `AuthService.cs`: reescritura completa — ya NO recibe `HttpClient` por DI (lo crea él mismo en el constructor, con el mismo `#if DEBUG` cert bypass que tenía `MauiProgram`, para evitar ambigüedad de DI con el `HttpClient` con handler de `ApiClient`); gana `InicializarAsync()` (lee `SecureStorage`), `LoginAsync(usuario,password)` (llama `POST api/auth/login`, guarda token+rol en `SecureStorage`), `Logout()`; se eliminan `Register`/`GetPassword`. Modify `MauiProgram.cs`: un solo `HttpClient` (`Scoped`) envuelto en `AuthHeaderHandler` para `ApiClient`; `AuthService` `Singleton` sin depender de ningún `HttpClient` de DI. Verificación: `dotnet build RentaFacil.MAUI -f net10.0-android`.

- **Task 19 — `Login.razor` simplificado.** Modify `Login.razor`: reescritura completa — se quitan las vistas "register"/"recover"; queda solo el form de login, `await Auth.LoginAsync(...)`. Modify `MainLayout.razor`: `OnInitializedAsync` hace `await Auth.InicializarAsync()` antes de chequear `IsAuthenticated` (se quita el `OnAfterRender` viejo no-async).

- **Task 20 — Verificación final completa.** `dotnet build RentaFacil.API` (no el `.slnx` completo, por el bug preexistente de build Android en Windows, confirmado con `git stash` que no lo causamos nosotros) + `dotnet test RentaFacil.Tests` (todo en verde) + smoke test manual end-to-end (login, CRUD con cédula válida/inválida, recibo PDF, 2do usuario no ve datos del 1ro, rate limit, cabeceras) + actualizar `CLAUDE.md` sección "Pendiente" (quitar puntos ya resueltos) y "Último Contexto" final.

**Cuidado con procesos huérfanos:** durante esta sesión, `dotnet run` en background dejó el `.dll`/`.exe` bloqueados varias veces. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
