# Flujo de trabajo

## Antes de tocar nada
1. Leer `CLAUDE.md` (raíz) — en particular las secciones "Pendiente" y "Último Contexto" para saber qué falta y dónde se dejó el trabajo.
2. Si la tarea toca seguridad, auditoría o multiusuario, leer `ClaudeCampeonatoatp.md` — ya tiene el análisis de qué patrones del proyecto hermano aplican aquí y cuáles no.
3. [PENDIENTE: branching planeado (`main` = desplegado, `develop` = betas, `feature/*` = características) pero el historial real (2 commits, ambos en `main`) no lo sigue todavía — confirmar con el usuario si se adopta.]

## Para hacer un cambio
1. Si el cambio toca un modelo de `RentaFacil.API/Models/`, actualizar también `AppDbContext.OnModelCreating` si afecta relaciones/restricciones, y generar una migración (`dotnet ef migrations add <Nombre>` desde `RentaFacil.API/`).
2. Si el cambio toca un DTO, actualizarlo en `RentaFacil.Shared/Models/` (no duplicar el shape en API y MAUI).
3. Seguir la capa `Model → Repository → Service → Controller`; no acceder a `AppDbContext` directo desde un Controller nuevo (la única excepción existente, `UnidadesController`, no es la plantilla a seguir — ver `convenciones.md`).
4. Si el cambio agrega lógica de negocio, agregar/actualizar el test del Service correspondiente en `RentaFacil.Tests/` siguiendo el patrón de mockear el Repository (ver `convenciones.md` → Tests). [PENDIENTE: no hay regla explícita de "tests primero" (TDD) documentada — no asumirla si el usuario no la pide.]
5. Si el cambio toca `RentaFacil.MAUI`, verificar si afecta a `ApiConfig.BaseUrl` (cambios de puerto/IP) o si requiere ajustar `Services/ApiClient.cs`.

## Antes de dar algo por terminado
- [ ] `dotnet build RentaFacil.slnx` sin errores
- [ ] `dotnet test RentaFacil.Tests` en verde
- [ ] Si se agregó/cambió un modelo: la migración EF Core correspondiente está creada y aplica limpio sobre la BD SQL Server (`dotnet ef database update`, o el `Migrate()` automático del arranque)
- [ ] La BD SQLite legacy (`rentafacil.db`) ya NO está versionada (está en `.gitignore`); confirmar que no reaparezca por accidente en `git status`
- [ ] [PENDIENTE: no hay linter/CI configurado para verificar automáticamente — no hay un comando adicional que correr.]

## Deploy (fases planeadas)
Versionado y reglas de respaldo: ver decisión "Versionado SemVer adaptado" en `decisiones.md`.

- **Fase 1 — Local (ACTUAL):** no hay deploy real. El "release" es generar un APK de prueba en `betas APKs/` y subir versión (`RentaFacilApp beta V1.0.X`). Base SQL Server local (instancia por máquina, connection string en user-secrets). Cada nuevo `.apk` ⇒ `git commit` (y opcionalmente `push`) del código exacto que lo generó.
- **Fase 2 — Render (planeada):** desplegar `RentaFacil.API` en Render (servidor de pruebas), con variables de entorno reales (CORS, connection string segura) y apuntando la app a la URL de Render. Se quita la etiqueta `beta` (`RentaFacilApp_V1.0.X`). Requisitos antes de pasar: compila sin errores y los secretos salen del código (`.env`).
- **Fase 3 — Oracle Cloud (planeada):** salto a `V2.0.X` por cambio de infraestructura. Instancia Compute Linux + Docker + SQL Server gestionado/contenedorizado, disponibilidad 24/7. Requisitos antes de pasar: superar pruebas de latencia en Render y tener listos los scripts de migración de BD para producción.

No hay scripts de deploy, `Dockerfile`/`docker-compose.yml` ni CI/CD en el repo todavía — todo Fase 2/3 está sin implementar.
