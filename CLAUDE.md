# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio.

> **Este archivo es solo un índice.** Da mini-resúmenes y enlaza al detalle. NO leas todo: lee la sección que necesites y, si requieres más, abre el `.md` enlazado. Responder siempre en español.

## En una frase
RentaFácil: app personal (aún de un solo usuario) para que un arrendador gestione inquilinos, inmuebles/unidades, contratos de alquiler y pagos, con recibos en PDF. Cliente **.NET MAUI Blazor Hybrid** (móvil + escritorio + web, páginas `.razor`) + backend **ASP.NET Core Web API** (.NET 10, EF Core, **SQL Server** local y producción con schemas `auth`/`renta`/`config`/`audit`). Código, comentarios y UI en español.

## Arranque rápido
- Solución: `RentaFacil.slnx` (no `.sln`). 4 proyectos: `RentaFacil.Shared` (DTOs/enums), `RentaFacil.API` (backend), `RentaFacil.MAUI` (cliente), `RentaFacil.Tests` (xUnit+Moq+FluentAssertions).
- Build: `dotnet build RentaFacil.slnx` · API: `dotnet run --project RentaFacil.API` (escucha en `http://0.0.0.0:5295`) · Tests: `dotnet test RentaFacil.Tests`.
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
- Multiusuario real: ASP.NET Identity + JWT, login con Google (OAuth 2.0).
- Dockerizar API + BD (SQL Server); deploy en Render → Oracle Cloud. (La migración a SQL Server con schemas ya está hecha — ver "Último Contexto".)
- Notificaciones de vencimiento de pago; compartir recibo por WhatsApp (deep link).
- Medidores de servicios (agua/luz) por unidad; módulo Ingresos con gráficas.
- Confirmar/implementar semántica de color del Estado de Pagos (ver glosario).
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web, firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-06-26
**Hecho hoy:** BD SQL Server configurada y verificada en la máquina de casa (`DESKTOP-07M16LE`):
1. `sqllocaldb` ya estaba instalado pero la instancia `MSSQLLocalDB` estaba detenida — se inició (`sqllocaldb start MSSQLLocalDB`). Nota: el nombre con sufijo dinámico que aparece en `decisiones.md`/`CLAUDE.md` antiguos (`...\LOCALDB#9246A1FB`) es solo el pipe interno de esa sesión, no algo que va en la connection string; la connection string correcta y estable es `Server=(localdb)\MSSQLLocalDB;...`.
2. User-secrets configurados en `RentaFacil.API` (no existían en esta máquina, era un clone nuevo): `ConnectionStrings:Default`, `Jwt:Key` (generada al azar, 64 chars), `SeedAdmin:Usuario`/`SeedAdmin:Password` = `admin`/`admin` (decisión del usuario, igual que el login local viejo).
3. `dotnet-ef` no estaba instalado globalmente en esta máquina → `dotnet tool install --global dotnet-ef`.
4. `dotnet restore RentaFacil.slnx` (faltaba `project.assets.json`) + `dotnet ef database update` desde `RentaFacil.API/` → migración `InitialSqlServer` aplicada limpio. Verificado por `sqlcmd`: los 4 schemas existen (`auth.Usuarios`, `config.__EFMigrationsHistory`, `renta.{Inquilinos,Inmuebles,Unidades,Contratos,Pagos}`; `audit` sigue siendo columnas `IAuditable` en `renta.*`, no un schema con tablas propias).
5. `dotnet build RentaFacil.API/RentaFacil.API.csproj` limpio, `dotnet test RentaFacil.Tests` 50/50 verdes. **Build de la `.slnx` completa falla** (`NETSDK1047`, no relacionado a la BD): al compilar la solución entera, MAUI intenta resolver `RentaFacil.API` para los RID `android-x64`/`android-arm64` y no los tiene — construir API y MAUI por separado (`dotnet build RentaFacil.API/...csproj`, `dotnet build RentaFacil.MAUI -f net10.0-android`) evita el problema, ver "Arranque rápido" en este archivo.
6. `dotnet run --no-build` (con `timeout 20`) levantó la API completa: migración ya al día, seed dummy + usuario admin insertados, escuchando en `0.0.0.0:5295`. Verificado que no quedó proceso huérfano en el puerto 5295 al terminar.

**Antecedentes (ya en `main`):**
- Plan de seguridad/auditoría (20 tasks): autenticación JWT+BCrypt, IDOR/BOLA cerrado en las 5 entidades, auditoría automática, cabeceras de seguridad HTTP, rate limiting, validación de cédula/RUC. Puntos 1-5 de `ClaudeCampeonatoatp.md` cubiertos.
- Plan de migración SQL Server (9 tasks): SQLite/MySQL reemplazados por SQL Server con 4 schemas (`auth`/`renta`/`config`/`audit`), `decimal(18,2)`, `IDbContextFactory` para futuro BD-por-tenant. Ya aplicada en ambas máquinas (trabajo y casa).
- Plan de globalización (9 tasks, rama `feature/globalizacion`, ya mergeada a `main`): `InvariantCulture` + `MoneyFormatter` (es-EC) + infraestructura `.resx` para evitar bugs de punto/coma decimal. 50 tests verdes. Detalle completo en el historial de commits (`b404fa2`..`b4f85f0`) y en `decisiones.md`.

**Próximo paso sugerido:** con la BD ya operativa en esta máquina, el foco pasa a **lógica y UX/UI** (sin spec todavía — usar `superpowers:brainstorming` cuando se aborde, no implementar directo).

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
