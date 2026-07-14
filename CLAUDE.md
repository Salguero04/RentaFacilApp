# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio.

> **Este archivo es solo un índice.** Da mini-resúmenes y enlaza al detalle. NO leas todo: lee la sección que necesites y, si requieres más, abre el `.md` enlazado. Responder siempre en español.

## En una frase
RentaFácil: app para que un arrendador gestione inquilinos, inmuebles/unidades, contratos de alquiler y pagos con recibos en PDF, y ahora con **dos perfiles de cuenta**: arrendador (`Administrador`/`Propietario`, la app completa) e **inquilino** (rol `Inquilino`: portal `/mi` de solo-su-data — contrato, pagos, recibos, consumos, notificaciones y reportar pagos —, se registra con el código QR que genera su arrendador). **Dos clientes que comparten una sola UI** (`RentaFacil.UI`, Razor Class Library): **.NET MAUI Blazor Hybrid** (móvil + escritorio) y **Blazor WebAssembly** (navegador). Backend **ASP.NET Core Web API** (.NET 10, EF Core, **SQL Server** local y producción con schemas `auth`/`renta`/`config`/`audit`). Código, comentarios y UI en español.

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

**Planes:**
- **Producción en Oracle Cloud** → docs/contexto/plan-produccion-oracle.md — **PENDIENTE de ejecutar.** Docker (SQL Server emulado amd64 en ARM), Cloudflare/HTTPS, deploy `update.sh`, correos Brevo + recuperación de contraseña, versionado/bloqueo de APK obsoleto. Incluye 2 decisiones abiertas del usuario (dominio propio vs DuckDNS+Caddy; GATE de SQL Server emulado). Sus puntos de integración con el módulo inquilino ya quedaron satisfechos del lado del módulo (email opcional en registro, SignalR por grupos, roles vs endpoints anónimos).
- **Módulo Inquilino + QR** → docs/contexto/plan-modulo-inquilino.md — **EJECUTADO 2026-07-14** (rama `feature/modulo-inquilino`, ver "Último Contexto").

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
- Multiusuario real: **el módulo inquilino ya existe** (2026-07-14: rol `Inquilino` con portal propio, registro self-service por QR, API cerrada por roles, SignalR por grupos — ver "Último Contexto"); queda pendiente el multiusuario de *arrendadores* (varios arrendadores independientes registrándose solos). Login con Google: **cimientos implementados** (2026-07-07) — falta solo crear credenciales en Google Cloud Console (user-secrets `Google:ClientId`, opcional `Google:PermitirRegistro`) e implementar `IProveedorGoogle` real por plataforma (hoy impl no-soportada, botón oculto).
- Dockerizar API + BD (SQL Server); deploy en Render → Oracle Cloud. (La migración a SQL Server con schemas ya está hecha — ver "Último Contexto".)
- Notificaciones automáticas/push de vencimiento de pago (los Recordatorios manuales —nota + fecha, sin push— ya están implementados, ver "Último Contexto" 2026-06-27).
- Deep link de WhatsApp al compartir recibo: `Home.razor.CompartirWhatsApp` hoy usa el share genérico de MAUI (`DataTransfer.Share`, el usuario elige la app destino), no abre WhatsApp directo con `wa.me`/`whatsapp://send`.
- Módulo Ingresos con **gráficas** (los servicios/medidores de agua/luz ya están implementados — ver "Último Contexto" 2026-06-28; falta solo la parte de gráficas/analítica).
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web con gráficas/analítica (el cliente web base ya existe — `RentaFacil.Web`, Blazor WASM; falta el módulo de reportes/gráficas), firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-07-14
**Commit:** `4ab0f23` en `main` (pusheado) — **Módulo Inquilino + QR completo y mergeado** (rama `feature/modulo-inquilino`, 16 commits, ya borrada; plan `docs/contexto/plan-modulo-inquilino.md`, tareas 1-12 + 7b, cada una con revisión aprobada; revisión final de integración Opus: **Ready to merge, cero Critical/Important**). 105/105 tests verdes; builds API/UI/Web/MAUI-android limpios.

**Antecedentes (ya en `main`):** 6 proyectos, seguridad/auditoría (JWT+BCrypt, IDOR cerrado, rate limiting), globalización, SQL Server 4 schemas, Medidores, CRUD completos, SignalR tiempo real (Pago/Contrato), cimientos Google OAuth (sin credenciales). Detalle en historial de git y `docs/contexto/`.

**Esta tarea — Módulo Inquilino + QR (2026-07-14, subagent-driven, 13 commits `b71c3c8..414f99e`):**
1. **Seguridad primero:** todos los controllers de arrendador ahora exigen `[Authorize(Roles=Administrador,Propietario)]` (antes bastaba estar autenticado — con cuentas de inquilinos habría sido un agujero).
2. **Modelo (migración `ModuloInquilino`):** `Inquilino.UsuarioCuentaId int?` (puente persona↔cuenta), `CodigoVinculacion` (código 8 chars único, expira 7 días, un solo uso con **reclamo atómico** `ExecuteUpdate WHERE UsadoEn IS NULL` anti-doble-uso) y `ReportePago` (Monto, Comentario, FotoComprobante ≤1MB varbinary, Estado Pendiente/Confirmado/Rechazado), todo en schema `renta`.
3. **Vinculación:** el arrendador genera código+QR por contrato (`POST api/contratos/{id}/codigo-vinculacion`, PNG vía QRCoder servido CON Bearer y mostrado como data URL); registro self-service `POST api/auth/registrar-inquilino` ([AllowAnonymous]+rate limit: código vigente → crea cuenta rol Inquilino + vincula + JWT; email opcional para futura recuperación de contraseña).
4. **Portal `api/mi/*`** (`[Authorize(Roles=Inquilino)]`, `MiPortalController`): contratos/pagos/recibo PDF/consumos/notificaciones (por fin hay consumidor de `NotificacionPendiente`)/vincular otro código/reportes-pago. TODO filtrado por la cadena cuenta→inquilinos→contratos derivada del token (revisiones Opus: 8/8 y 9/9 invariantes anti-IDOR).
5. **Reportes de pago:** inquilino reporta (monto+comentario+foto), arrendador confirma/rechaza desde `/reportes-pago` (bandeja nueva con refresco SignalR en vivo); confirmar NO crea el Pago automático — botón directo a CrearPago.
6. **SignalR por grupos:** `DatosHub.OnConnectedAsync` mete cada conexión en `usuario-{id}` y `DataChangeNotifier` emite a `Clients.Group` (ya no `Clients.All`) — los inquilinos no ven eventos del arrendador ni viceversa.
7. **UI:** enrutado por rol (`AuthService.EsInquilino`: NavMenu con rama inquilino, login redirige a `/mi`), 6 pantallas de inquilino (`RegistroInquilino` con escáner QR nativo —ZXing.Net.Maui, permiso CAMERA, abstracción `IEscanerQr` con no-soportado en Web—, `MiPortal`, `MisPagos`, `MisConsumos`, `MisNotificaciones`, `ReportarPago` con InputFile ≤1MB) + lado arrendador (modal QR en `Contratos.razor`, bandeja `ReportesPago.razor`).

**Próximo paso:** prueba manual end-to-end del flujo (guía en "Verificación end-to-end" del plan): generar QR desde Contratos → registrar inquilino con el código (escaneo en Android o manual) → ver portal `/mi` → reportar pago → confirmarlo en vivo en `/reportes-pago`. Los 12 minors de las revisiones quedaron triageados "merge así" (detalle en `.superpowers/sdd/progress.md`); limpieza aplicada: `WeatherForecastController` eliminado, NU1903 anotado en el plan de producción. Backlog sin cambios: plan de producción Oracle (pendiente), k6, gráficas, credenciales Google.


**Gotchas vigentes:** (a) `ServiciosController.cs` contiene la clase `MedidoresController` (nombre archivo↔clase desalineado). (b) `MedidorInquilino.ContratoId` y `Recordatorio.ContratoId` son informativos (sin FK estricta); también `CodigoVinculacion` y `ReportePago` referencian Contrato/Inquilino sin FK estricta. (c) `dotnet build RentaFacil.slnx` completo falla con NETSDK1047 (multi-RID de MAUI) — buildear proyectos individuales o correr `dotnet test`. (d) credenciales de login: user-secrets `SeedAdmin:Usuario`/`SeedAdmin:Password` (no hay `admin/admin`). (e) borrar un Contrato cascada-borra sus Pagos a nivel BD sin pasar por `PagoService` → NO se emite evento SignalR `"Pago"`. (f) ya RESUELTOS (2026-07-14, módulo inquilino): los eventos SignalR van por grupos `usuario-{id}` (antes `Clients.All`) y `NotificacionPendiente` ya tiene consumidor (portal `api/mi/notificaciones`). (g) confirmar un `ReportePago` NO crea el Pago — flujo manual vía CrearPago a propósito. (h) el código QR es un secreto de un solo uso: el PNG se sirve autenticado (data URL en la UI); no exponerlo anónimo.

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. API en puerto 5295, web en 5213. Antes de rebuildear: `netstat -ano | grep <puerto>` (Bash tool) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`.
