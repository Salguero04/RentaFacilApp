# Decisiones tomadas

> Una entrada por decisión, inferida del código/docs del repo. Donde no hay evidencia explícita del "por qué" o de las alternativas descartadas, se deja marcado `[PENDIENTE]` en vez de inventarlo.

## SQLite en local / MySQL en producción
- **Decisión:** EF Core con SQLite (`Data Source=rentafacil.db`) en Fase 1 local; el destino de producción es MySQL.
- **Por qué:** sin necesidad de servidor de base de datos para una app de uso personal en desarrollo; MySQL es la opción para cuando se despliegue en Render/Oracle Cloud.
- **Descartado:** SQL Server (se barajó como opción para Fase 2, pero el diseño final apunta a MySQL; no hay código que use SQL Server).
- **Estado:** vigente para Fase 1 (SQLite). La migración a MySQL en producción aún no está implementada.

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
