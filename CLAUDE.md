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
- **Decisiones** → @docs/contexto/decisiones.md — SQL Server con schemas (auth/renta/config/audit) + `IDbContextFactory` para futuro BD-por-tenant, `LOCAL` compile constant para la URL de la API, CORS abierto + HTTPS off (LAN), migración automática, versionado SemVer + respaldo por APK.
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
**Plan ejecutado:** `docs/superpowers/plans/2026-06-26-migracion-sqlserver.md` (9 tasks), ejecución inline (no subagentes) en la rama `feature/migracion-sqlserver` (creada desde `main`). **Mergeada a `main` con fast-forward y pusheada a GitHub** (`c6278ce`); la rama se borró.

**Antecedente (ya en `main`):** el plan de seguridad/auditoría (`docs/superpowers/plans/2026-06-26-seguridad-auditoria.md`, 20 tasks) está completo y mergeado a `main` con fast-forward — autenticación real JWT+BCrypt, IDOR/BOLA cerrado en las 5 entidades + recibo PDF, auditoría automática, cabeceras de seguridad HTTP, rate limiting de login y validación de cédula/RUC. El detalle por commit vive en el git log; los puntos 1-5 de `ClaudeCampeonatoatp.md` quedaron cubiertos.

**Migración SQL Server — Tasks 1-9 COMPLETAS y commiteadas en `feature/migracion-sqlserver`** (decisión confirmada por el usuario: SQL Server reemplaza a SQLite y a MySQL en todos los entornos). Verificado end-to-end contra el SQL Server de la máquina de trabajo (`GGCBOADMWRK025\SQLEXPRESS`, SQL Server 2025 Express): la API arranca con BD vacía, EF crea la BD `RentaFacil`, los schemas (`auth`/`renta`/`config`), las tablas en su schema (`auth.Usuarios`, las 5 de dominio en `renta.*`, `config.__EFMigrationsHistory`), siembra el admin y los datos dummy; smoke test HTTP OK (401 sin token, login con JWT, GET con datos, 201/400 según cédula, recibo PDF real, 2do usuario ve `[]` → IDOR sigue cerrado). 37 tests verdes. Detalle por task:
1. `Microsoft.EntityFrameworkCore.SqlServer` agregado, `...Sqlite` quitado. `Program.cs`: provider → `UseSqlServer(connectionString, sqlOpt => sqlOpt.MigrationsHistoryTable("__EFMigrationsHistory", "config"))`, connection string desde `ConnectionStrings:Default` (config/user-secrets). El interceptor de auditoría se preserva en el mismo `AddDbContext`.
2. `AppDbContext.OnModelCreating`: `ToTable(..., "auth"/"renta")` por entidad + `HasIndex(x => x.UsuarioId)` en las 5 entidades de `renta.*` (SQL Server no indexa eso solo).
3. `[Column(TypeName = "decimal(18,2)")]` en los 7 campos monetarios (Contrato.Monto/Garantia, Pago.TotalMonto/ACuenta/Servicios, Inmueble.MontoRenta, Unidad.MontoRenta).
4. `rm -rf Migrations/` (las 6 migraciones SQLite ya no aplican; quedan en git history) + `dotnet ef migrations add InitialSqlServer`. Verificada: `EnsureSchema` auth/renta, tablas en su schema, 7×`decimal(18,2)`, 5 índices `UsuarioId`.
5. `appsettings.json` gana `ConnectionStrings:Default` vacío (template); el guard en `Program.cs` pasó de `?? throw` a `string.IsNullOrWhiteSpace` (un placeholder vacío dispara el mensaje de ayuda hacia user-secrets). Connection string real por máquina vía `dotnet user-secrets set "ConnectionStrings:Default" ...`.
6. `Program.cs`: `AddDbContextFactory<AppDbContext>` registrado como base para el futuro `TenantDbContextFactory` (BD-por-tenant). No cambia el comportamiento de hoy.
7. **Gotcha real:** `AddDbContextFactory` por defecto es Singleton y no puede consumir las `DbContextOptions` Scoped que registra `AddDbContext` → `InvalidOperationException` al arrancar ("Cannot consume scoped service ... from singleton"). Fix: registrar el factory con `ServiceLifetime.Scoped` (3er argumento). Esto NO lo cachea el build — solo aparece al correr la API; verificarlo siempre arrancando, no solo con `dotnet build`.
8. `AuditoriaInterceptorTests` migrado de `SqliteConnection(":memory:")` a `UseInMemoryDatabase` (paquete `Microsoft.EntityFrameworkCore.InMemory` agregado a `RentaFacil.Tests`). El interceptor opera sobre el `ChangeTracker`, agnóstico del provider, así que los 3 tests siguen pasando. InMemory NO valida schemas ni `decimal(18,2)` — eso se verifica end-to-end contra SQL Server real.
9. Verificación final + docs. **Cleanup extra:** `rentafacil.db` (SQLite legacy) se sacó del control de versiones (`git rm --cached`) y `RentaFacil.API/*.db`/`-shm`/`-wal` se agregaron a `.gitignore` — cierra el gotcha de `errores-conocidos.md`. Docs actualizados (CLAUDE.md, `docs/contexto/` arquitectura/decisiones/errores-conocidos/flujo-de-trabajo, README) de SQLite/MySQL → SQL Server.

**Próximo paso sugerido:** **pendiente del usuario (no automatizable desde aquí):** aplicar la migración en la máquina de casa (`DESKTOP-07M16LE\LOCALDB#9246A1FB`) — configurar su user-secret `ConnectionStrings:Default` y arrancar la API una vez para que el `Migrate()` cree la BD y los schemas allí también. Aparte: se corrigió una inconsistencia en `docs/contexto/arquitectura.md` y `errores-conocidos.md` que seguían describiendo el estado pre-seguridad (sin auth/IDOR/auditoría) aunque ya estaban implementados — ver la nueva regla en "Convenciones de actualización de docs" más abajo.

**Cuidado con procesos huérfanos:** durante esta sesión, `dotnet run` en background dejó el `.dll`/`.exe` bloqueados varias veces. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
