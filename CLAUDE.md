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
**Hecho:** Implementando el plan de Seguridad+Auditoría (`docs/superpowers/specs/2026-06-26-seguridad-auditoria-design.md`, plan en `docs/superpowers/plans/2026-06-26-seguridad-auditoria.md`) en la rama `feature/seguridad-auditoria`. Completadas las Tasks 1-5 de 20: entidad `Usuario` + `AppRoles` + `IUsuarioRepository` (migración `AddUsuarios`), DTOs de auth (`LoginDto`/`LoginResultDto`/`RegistrarUsuarioDto`), `AutenticacionService` (BCrypt + JWT, 5 tests en verde), `AuthController` (`POST /api/auth/login` anónimo, `POST /api/auth/registrar` solo Administrador) y wiring de JWT Bearer + fallback policy en `Program.cs` — confirmado manualmente que `/api/inquilinos` ahora devuelve `401` sin token. `Jwt:Key` configurado vía `dotnet user-secrets` (no hardcodeado).
**Siguiente paso sugerido:** seguir con Task 6 (cabeceras de seguridad HTTP) en adelante, según `docs/superpowers/plans/2026-06-26-seguridad-auditoria.md`. Hasta la Task 9 (siembra del usuario dueño) no hay forma de loguearse de verdad — es esperado, no es un bug.
**Sin commitear:** nada — cada task se commitea individualmente en `feature/seguridad-auditoria` según avanza. La rama no está mergeada a `main`.
