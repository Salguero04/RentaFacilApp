# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio.

> **Este archivo es solo un índice.** Da mini-resúmenes y enlaza al detalle. NO leas todo: lee la sección que necesites y, si requieres más, abre el `.md` enlazado. Responder siempre en español.

## En una frase
RentaFácil: app personal (aún de un solo usuario) para que un arrendador gestione inquilinos, inmuebles/unidades, contratos de alquiler y pagos, con recibos en PDF. **Dos clientes que comparten una sola UI** (`RentaFacil.UI`, Razor Class Library): **.NET MAUI Blazor Hybrid** (móvil + escritorio) y **Blazor WebAssembly** (navegador). Backend **ASP.NET Core Web API** (.NET 10, EF Core, **SQL Server** local y producción con schemas `auth`/`renta`/`config`/`audit`). Código, comentarios y UI en español.

## Arranque rápido
- Solución: `RentaFacil.slnx` (no `.sln`). 6 proyectos: `RentaFacil.Shared` (DTOs/enums/MoneyFormatter), `RentaFacil.UI` (RCL con las pantallas `.razor` compartidas), `RentaFacil.API` (backend), `RentaFacil.MAUI` (host móvil/escritorio), `RentaFacil.Web` (host Blazor WASM), `RentaFacil.Tests` (xUnit+Moq+FluentAssertions). La UI vive en `RentaFacil.UI`; MAUI y Web solo aportan host + impls de plataforma (`Platform/`). Ver `docs/contexto/arquitectura.md`.
- Build: `dotnet build RentaFacil.slnx` · API: `dotnet run --project RentaFacil.API` (escucha en `http://0.0.0.0:5295`) · Tests: `dotnet test RentaFacil.Tests`.
- **Web (navegador):** con la API corriendo, `dotnet run --project RentaFacil.Web --launch-profile http` (sirve en `http://localhost:5213`; perfil `http` a propósito, para no chocar con la API HTTP por mixed-content). La URL de la API está en `RentaFacil.Web/Program.cs` (`apiBaseUrl`, hoy `http://localhost:5295`). No requiere el workload `wasm-tools` para correr en dev.
- Test único: `dotnet test RentaFacil.Tests --filter "FullyQualifiedName~InquilinoServiceTests.CrearAsync_ShouldReturnCreatedInquilinoDto"`.
- BD: **SQL Server** (local y prod). La connection string va por máquina en user-secrets: `dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=RentaFacil;Integrated Security=true;TrustServerCertificate=true;" --project RentaFacil.API` (sin esto la API/EF lanzan un error claro al arrancar). Trabajo: `GGCBOADMWRK025\SQLEXPRESS`. Casa: `DESKTOP-07M16LE\LOCALDB#9246A1FB`.
- Migraciones (desde `RentaFacil.API/`): `dotnet ef migrations add <Nombre>` / `dotnet ef database update`. Se aplican solas al arrancar la API.
- MAUI: algunos TFM solo compilan en su SO (iOS/MacCatalyst en macOS, `windows` en Windows). Android en Windows: `dotnet build RentaFacil.MAUI -f net10.0-android`.

## Contexto del proyecto
Cada eje en su archivo — abre solo el que necesites:

- **Arquitectura** → @docs/contexto/arquitectura.md — stack, mapa de carpetas, flujo de datos (Blazor → `ApiClient` → Controller → Service → Repository → EF Core), esquema de BD y reglas de borrado, y "lo que NO existe".
- **Convenciones** → @docs/contexto/convenciones.md — idioma español, naming, DTOs `record` en `Shared`, capas `Model→Repository→Service→Controller`, páginas `.razor` (no `.cshtml`), bottom sheet en móvil, tests de Services con repo mockeado.
- **Decisiones** → @docs/contexto/decisiones.md — SQL Server con schemas (auth/renta/config/audit) + `IDbContextFactory` para futuro BD-por-tenant, `InvariantCulture`+`MoneyFormatter` es-EC+infraestructura `.resx`, `LOCAL` compile constant para la URL de la API, CORS abierto + HTTPS off (LAN), migración automática, versionado SemVer + respaldo por APK.
- **Glosario** → @docs/contexto/glosario.md — términos del dominio (Inmueble Único/Múltiple, Unidad, Contrato, Pago, Periodo, Facturado/Completado), entidades con sus campos, indicadores de color del Estado de Pagos, siglas internas.
- **Flujo de trabajo** → @docs/contexto/flujo-de-trabajo.md — pasos para un cambio, checklist de "terminado", y las 3 fases de deploy (Local actual / Render / Oracle Cloud).
- **Errores conocidos** → @docs/contexto/errores-conocidos.md — `UnidadesController` salta capas, IP de prod hardcodeada, IDOR y login local (ya RESUELTOS), y cosas que parecen rotas pero son a propósito (CORS/HTTPS, seed dummy).

### Regla: verificar `docs/contexto/` al cerrar cualquier tarea
**Motivo:** ya pasó dos veces — `arquitectura.md` ("Lo que NO existe") y `errores-conocidos.md` seguían describiendo auth/IDOR/auditoría como ausentes mucho después de haberse implementado y mergeado, porque al cerrar esas tareas solo se actualizó `CLAUDE.md` y las secciones "positivas" de esos mismos archivos, nunca las listas de cierre.

Antes de dar por terminada cualquier tarea que resuelva algo descrito como pendiente/ausente/gotcha en estos docs:
1. `grep` (o búsqueda manual) de palabras clave del problema resuelto en **todos** los `.md` de `docs/contexto/` + `CLAUDE.md` + `ClaudeCampeonatoatp.md` — no asumir que solo vive en el archivo "obvio".
2. Revisar con especial cuidado las **secciones de cierre/listas negativas**: "Lo que NO existe" en `arquitectura.md` y cada entrada de `errores-conocidos.md` — son las que más se desactualizan porque viven separadas de donde ocurre el cambio real.
3. Si una entrada queda resuelta, no borrarla en silencio: marcarla **"ya RESUELTO"** con fecha y qué la resolvió (commit/rama/plan), igual que se hizo con `rentafacil.db` y con IDOR/login.
4. Esto aplica también a "Último Contexto" de este archivo: si dice "la rama no se ha mergeado" y ya se mergeó, corregirlo en el mismo checkpoint que hace el merge — no dejarlo para una pasada posterior.

## Pendiente
Lista de lo que falta implementar. El análisis de seguridad/auditoría a fondo (con código de referencia del proyecto hermano CampeonatoATP) vive en → @ClaudeCampeonatoatp.md.

**Seguridad/auditoría — implementado y mergeado a `main` (ver "Último Contexto"):** filtrado por `UsuarioId` (IDOR/BOLA cerrado en las 5 entidades), auditoría de cambios (`IAuditable`+`AuditoriaInterceptor`), cabeceras de seguridad HTTP, validación de cédula/RUC, y autenticación real (JWT+BCrypt+rate limiting). Esto cubre los puntos 1-5 del orden de prioridad de @ClaudeCampeonatoatp.md. Queda pendiente solo:
1. Pruebas de carga k6 antes de escalar usuarios (punto 6 de `ClaudeCampeonatoatp.md`).

**Funcionalidad (Fase 2 / Fase 3, del plan de producto):**
- Multiusuario real: ASP.NET Identity + JWT, login con Google (OAuth 2.0). **Los cimientos del login con Google ya están implementados** (2026-07-07, ver "Último Contexto"): falta solo crear las credenciales en Google Cloud Console (user-secrets `Google:ClientId`, opcional `Google:PermitirRegistro`) e implementar `IProveedorGoogle` real por plataforma (hoy hay una impl no-soportada y el botón queda oculto).
- Dockerizar API + BD (SQL Server); deploy en Render → Oracle Cloud. (La migración a SQL Server con schemas ya está hecha — ver "Último Contexto".)
- Notificaciones automáticas/push de vencimiento de pago (los Recordatorios manuales —nota + fecha, sin push— ya están implementados, ver "Último Contexto" 2026-06-27).
- Deep link de WhatsApp al compartir recibo: `Home.razor.CompartirWhatsApp` hoy usa el share genérico de MAUI (`DataTransfer.Share`, el usuario elige la app destino), no abre WhatsApp directo con `wa.me`/`whatsapp://send`.
- Módulo Ingresos con **gráficas** (los servicios/medidores de agua/luz ya están implementados — ver "Último Contexto" 2026-06-28; falta solo la parte de gráficas/analítica).
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web con gráficas/analítica (el cliente web base ya existe — `RentaFacil.Web`, Blazor WASM; falta el módulo de reportes/gráficas), firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-07-07
**Commit:** `b2851c8` en `main` (pusheado) — **Fases 2 (SignalR) y 3 (cimientos Google OAuth) mergeadas** (rama `feature/signalr-google-oauth`, 9 commits, ya borrada; revisión final de integración Opus: Ready to merge). Antes, en esta misma sesión: **Fase 1 "arreglar todos los CRUD" mergeada** en `94f3836` (rama `feature/crud-fixes-signalr`, borrada). Plan de referencia: `~/.claude/plans/comencemos-con-hacer-funcionar-quirky-gadget.md`.

**Antecedentes (ya en `main`):** 6 proyectos (`Shared`/`API`/`UI`/`MAUI`/`Web`/`Tests`), seguridad/auditoría (JWT+BCrypt, IDOR/BOLA cerrado, auditoría automática, rate limiting), globalización, SQL Server con 4 schemas, y el rediseño de Medidores (entidad propia + 3 métodos de cobro + edición de contratos, 2026-06-29, `06f7e56`). Detalle en el historial de git.

**Esta tarea — Fase 1: CRUD completos en toda la app (2026-07-07, mergeada):**
1. **Patrón base (Inquilino):** `CrearInquilino.razor` ahora valida el `bool` devuelto por el ApiClient antes de navegar y muestra `errorMessage`; `<DataAnnotationsValidator/>` activa la validación de cédula en cliente. `InquilinoService.UpdateAsync` devuelve `Task<bool>` y el controller responde 404/204 (patrón de `ContratoService`). El disparador real del bug era el seed con cédula inválida `"1234567"` → corregido a `"1710034065"` en `Program.cs` + UPDATE puntual en la BD local.
2. **Edición de Pago:** `CrearPago.razor` acepta segunda ruta `/crearpago/{ContratoId:int}/{Id:int}`; en edición **preserva** `TotalMonto`/`Servicios`/`Periodo` del pago original (no los recalcula de medidores/renta actuales — hallazgo Important de la revisión final, corregido en `0694f40`). `PagoService.UpdateAsync` ignora `dto.Detalles` (no hay reemplazo de detalles al editar). Botón Editar en `DetallePagos.razor`.
3. **Recordatorio:** Update completo (Repository→Service→Controller→ApiClient con ownership doble: recordatorio + inquilino) y **pantalla nueva** `Recordatorios.razor` (`/recordatorios`, entrada en NavMenu con `bi-bell`) para listar/editar/eliminar.
4. **MedidorInquilino:** `ActualizarVinculoAsync` (triple ownership: vínculo+medidor+inquilino), `PUT api/medidores/inquilinos/{id}`, botón Editar por vínculo en `Medidores.razor`.
5. **Contrato:** `DeleteContratoAsync` en ApiClient + botón eliminar con modal de confirmación en `Contratos.razor`.
6. **Tests: 68/68 verdes.** Revisión final de rama (Opus): arquitectura/auth/IDOR/capas OK, veredicto "With fixes" ya aplicado.

**Fase 2 — SignalR (tiempo real, implementada 2026-07-07):** hub `DatosHub` (`[Authorize]`, endpoint `/hubs/datos`); el JWT viaja por query string `?access_token=` SOLO para paths `/hubs/*` (`OnMessageReceived` en `Program.cs` — las rutas `/api` no cambian). `IDataChangeNotifier`/`DataChangeNotifier` (best-effort: try/catch + `ILogger`, un fallo de SignalR nunca falla la operación ya persistida) emite evento `"CambioDatos"(entidad, usuarioId, accion)` a `Clients.All` desde `PagoService`/`ContratoService` (Crear/Update/Delete). Cliente: `SignalRClient` en `RentaFacil.UI/Services` (WithAutomaticReconnect, token de `ITokenStore`), registrado Singleton en MAUI y Scoped en Web; `Pagos.razor` se suscribe (filtra `"Pago"`, refresca con `InvokeAsync`). Alcance actual: solo Pago y Contrato notifican, y solo `Pagos.razor` escucha.

**Fase 3 — Cimientos login Google OAuth (implementada 2026-07-07, SIN credenciales aún):** `Usuario.GoogleId` (índice único filtrado) + `PasswordHash` nullable (migración `AgregarGoogleIdUsuario`), paquete `Google.Apis.Auth` solo en la API, `IValidadorTokenGoogle` mockeable (`GoogleTokenInfo` incluye `EmailVerified`), `AutenticacionService.LoginGoogleAsync` (matching `GoogleId` → `Email` **solo si `EmailVerified`** —anti account-takeover— → auto-registro solo si `Google:PermitirRegistro=true` con dedupe de `NombreUsuario`), `POST api/auth/login-google` ([AllowAnonymous]+rate limit; 503 sin `Google:ClientId`, 403 registro no permitido, 401 genérico). UI: `IProveedorGoogle` (abstracción) con `ProveedorGoogleNoSoportado` en ambos hosts → botón "Continuar con Google" oculto. **Para activarlo:** crear credenciales en Google Cloud Console, `dotnet user-secrets set "Google:ClientId" "<id>" --project RentaFacil.API` (+ opcional `Google:PermitirRegistro=true`), e implementar `IProveedorGoogle` real por plataforma.

**Verificación:** 84/84 tests verdes; builds API/UI/Web limpios; revisiones por fase (Opus) + fixes aplicados (`2638bef` robustez SignalR, `bc3fc7c` seguridad email_verified); revisión final de integración: Ready to merge. **No probado manualmente:** el flujo tiempo real end-to-end con dos clientes reales (requiere API + MAUI + Web corriendo a la vez) — hacerlo en la próxima sesión con la guía de verificación del plan.

**Próximo paso:** prueba manual del tiempo real con dos clientes a la vez (API + MAUI + Web; guía en la sección "Verificación end-to-end — Fase 2" del plan); pendientes del backlog sin cambios (k6, Docker, gráficas, etc.).

**Gotchas vigentes:** (a) `ServiciosController.cs` contiene la clase `MedidoresController` (nombre archivo↔clase desalineado). (b) `MedidorInquilino.ContratoId` y `Recordatorio.ContratoId` son informativos (sin FK estricta); al editar un vínculo el `ContratoId` queda null. (c) `NotificacionPendiente` sigue sin consumidor. (d) `dotnet build RentaFacil.slnx` completo falla con NETSDK1047 (multi-RID de MAUI) — buildear proyectos individuales (`RentaFacil.API`, `RentaFacil.UI`) o correr `dotnet test`. (e) credenciales de login: las siembra user-secrets `SeedAdmin:Usuario`/`SeedAdmin:Password` (no hay `admin/admin`). (f) borrar un Contrato cascada-borra sus Pagos a nivel BD sin pasar por `PagoService` → NO se emite evento SignalR `"Pago"`, el otro cliente no refresca su lista de pagos en ese caso. (g) eventos SignalR van a `Clients.All` sin agrupar por usuario — aceptable single-arrendador, agrupar antes de multiusuario real.

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. API en puerto 5295, web en 5213. Antes de rebuildear: `netstat -ano | grep <puerto>` (Bash tool) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`.
