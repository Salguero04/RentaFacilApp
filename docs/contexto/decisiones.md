# Decisiones tomadas

> Una entrada por decisión, inferida del código/docs del repo. Donde no hay evidencia explícita del "por qué" o de las alternativas descartadas, se deja marcado `[PENDIENTE]` en vez de inventarlo.

## SQL Server (local + producción) con schemas organizacionales
- **Decisión:** EF Core con SQL Server en todos los entornos. Las tablas se organizan en 4 schemas fijos (no por tenant): `auth` (identidad), `renta` (dominio, filtrado por `UsuarioId` por fila), `config` (catálogos globales + `__EFMigrationsHistory`) y `audit` (trazabilidad, hoy como columnas `IAuditable` en `renta.*`). Connection string por máquina vía user-secrets (`ConnectionStrings:Default`, `Integrated Security=true` en local). Migración única `InitialSqlServer`. Plan: `docs/superpowers/plans/2026-06-26-migracion-sqlserver.md`.
- **Por qué:** SQL Server ya está instalado en las máquinas de desarrollo (trabajo y casa); los schemas dan organización lógica y dejan lista la base para un futuro salto a BD-por-tenant (un `TenantDbContextFactory` que elija la connection string según el JWT — `IDbContextFactory` ya está registrado) sin tocar repositories/services/controllers.
- **Descartado:** **SQLite** (era la BD local de Fase 1, `Data Source=rentafacil.db`) y **MySQL** (era el destino de producción planeado) — ambos reemplazados por SQL Server el 2026-06-26. SQLite ignoraba `decimal(18,2)` y no soporta schemas reales; mantener dos providers (local vs prod) añadía fricción.
- **Estado:** vigente. Verificado end-to-end en la máquina de trabajo (`GGCBOADMWRK025\SQLEXPRESS`): migración aplicada, schemas/tablas creados, seed y smoke test HTTP OK. Pendiente: aplicar en la máquina de casa (`DESKTOP-07M16LE\LOCALDB`) y definir el hosting de producción.

## `LOCAL` compile constant para la URL de la API en MAUI
- **Decisión:** `ApiConfig.cs` usa `#if LOCAL` (definido automáticamente en builds Debug vía `DefineConstants` en `RentaFacil.MAUI.csproj`) para elegir entre la IP de loopback del emulador Android (`10.0.2.2:5295`) y una IP fija de LAN/producción en Release.
- **Por qué:** evitar tener que reconfigurar la URL a mano cada vez que se cambia entre probar en emulador y probar en un dispositivo real/LAN.
- **Descartado:** [PENDIENTE — no hay evidencia en el repo de que se evaluara un `appsettings.json`/config en runtime en vez de un compile constant.]
- **Estado:** vigente. Riesgo conocido: la IP de producción está hardcodeada en código (ver `errores-conocidos.md`).

## CORS abierto y HTTPS redirection deshabilitado en la API
- **Decisión:** `Program.cs` registra una política CORS `AllowAnyOrigin/AllowAnyMethod/AllowAnyHeader` y comenta explícitamente `app.UseHttpsRedirection()` con la nota "Comentado para permitir conexiones HTTP desde el celular en LAN".
- **Por qué:** permitir que el cliente MAUI en un celular real (sin certificado TLS de confianza) se conecte a la API corriendo en la misma LAN.
- **Descartado:** exigir HTTPS con certificado autofirmado o configurar `mkcert`/similar — no hay evidencia de que se haya intentado.
- **Estado:** vigente mientras todo corra en LAN. **Revisar antes de exponer la API a internet** (Fase 2 — Render, ver fases de despliegue en `flujo-de-trabajo.md`); no es aceptable en producción.

## Login local en MAUI sin tocar la API
- **Decisión:** `AuthService.cs` valida credenciales contra `Preferences` del dispositivo, con un usuario hardcodeado `admin/admin`. El comentario en el propio código dice *"Simple hardcoded user for now as requested"*.
- **Por qué:** el comentario indica que fue un pedido explícito para tener algo simple temporalmente, no una decisión de arquitectura definitiva.
- **Descartado:** [PENDIENTE — no hay alternativa documentada evaluada.]
- **Estado:** **pendiente de reemplazo**, no es una decisión final. Ver sección "Pendiente" de `CLAUDE.md` y `ClaudeCampeonatoatp.md` para el camino sugerido (BCrypt + auth real en la API) cuando se decida abordarlo.

## Migraciones aplicadas automáticamente al iniciar
- **Decisión:** `Program.cs` llama `context.Database.Migrate()` dentro de un scope al arrancar la API, en vez de requerir `dotnet ef database update` manual antes de cada `dotnet run`.
- **Por qué:** simplifica el flujo de desarrollo solo (ejecutar `dotnet run` siempre deja la base al día).
- **Descartado:** [PENDIENTE]
- **Estado:** vigente. Riesgo a tener en cuenta si se pasa a producción con datos reales: una migración con pérdida de datos se aplicaría sola al desplegar.

## Versionado SemVer adaptado + respaldo por APK
- **Decisión:** versionado `X.Y.Z` (X = cambio de arquitectura/servidor; Y = funcionalidad nueva; Z = bugfix/optimización). Las builds locales llevan etiqueta `beta` (`RentaFacilApp beta V1.0.X`); al subir a un entorno real se quita la etiqueta `beta`. Cada vez que se genera un nuevo `.apk` y se incrementa la versión, se hace `git commit` (y opcionalmente `push`) del código exacto que originó ese APK.
- **Por qué:** mantener un respaldo trazable en GitHub del estado que produjo cada APK distribuido.
- **Descartado:** [PENDIENTE]
- **Estado:** vigente. La estrategia de ramas planeada (`main` = desplegado, `develop` = betas, `feature/*` = características) está descrita pero el historial real (2 commits, ambos en `main`) aún no la sigue — ver `flujo-de-trabajo.md`.
