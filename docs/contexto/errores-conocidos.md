# Errores conocidos (gotchas)

> Trampas confirmadas leyendo el código real, no suposiciones. Si algo no se pudo confirmar, queda marcado `[PENDIENTE]` en vez de afirmado a ciegas.

## IDOR/BOLA por falta de filtro `UsuarioId` — ya RESUELTO
- **Era:** ningún Repository/Service/Controller filtraba por `UsuarioId` al leer/editar — cualquier usuario podía ver/modificar datos de otro.
- **Resuelto (2026-06-26, rama `feature/seguridad-auditoria` mergeada a `main`):** las 5 entidades (Inquilino/Inmueble/Unidad/Contrato/Pago) filtran por `UsuarioId` en cada `GetAllAsync`/`GetByIdAsync`/`Update`/`Delete` a nivel Repository, con índice en SQL Server. Verificado con un segundo usuario real que no ve ningún dato del primero.

## Login que no protege la API — ya RESUELTO
- **Era:** `AuthService.cs` en MAUI validaba contra `Preferences` local (usuario hardcodeado `admin/admin`) sin llamar a la API; la API no tenía `[Authorize]` en ningún Controller.
- **Resuelto (2026-06-26):** autenticación real JWT + BCrypt (`AuthController`, `AutenticacionService`), `AddAuthorization` con `FallbackPolicy = RequireAuthenticatedUser()` (todo endpoint requiere token salvo que se marque lo contrario), rate limiting en `/api/auth/login`. El cliente MAUI (`AuthService.cs` + `AuthHeaderHandler`) llama a la API real y adjunta el `Bearer` token.

## `UnidadesController` rompe el patrón de capas sin avisar
- **Pasa cuando:** se usa `UnidadesController` (en `OtherControllers.cs`) como referencia para escribir un Controller nuevo.
- **Causa real:** es el único Controller que inyecta `AppDbContext` directo en vez de un Service/Repository — parece un atajo de desarrollo, no hay justificación documentada.
- **Solución:** no copiar este patrón en código nuevo; si se tiene tiempo, refactorizarlo a Repository/Service como el resto.

## La IP de producción está hardcodeada en el código del cliente
- **Pasa cuando:** la IP de la LAN/servidor cambia y la app sigue apuntando a la vieja en builds Release.
- **Causa real:** `RentaFacil.MAUI/Config/ApiConfig.cs` tiene la URL de producción escrita literal en el código (`http://200.126.17.232:5295`), seleccionada vía el compile constant `LOCAL` (definido solo en Debug).
- **Solución:** al cambiar de red/servidor, actualizar esa línea y recompilar — no hay configuración en runtime todavía.

## `rentafacil.db` (SQLite, legacy) — ya RESUELTO
- **Era:** el archivo SQLite `RentaFacil.API/rentafacil.db` vivía trackeado en git y cambiaba solo con cada `dotnet run` (migraciones + seed), ensuciando `git status`.
- **Resuelto (2026-06-26):** al migrar a SQL Server (ver `decisiones.md`) SQLite dejó de usarse. El archivo se sacó del control de versiones (`git rm --cached`) y `RentaFacil.API/*.db`/`-shm`/`-wal` están en `.gitignore`. Ya no hay un binario de BD versionado.

## Cosas que parecen rotas pero son a propósito
- **CORS abierto (`AllowAnyOrigin/Method/Header`) y `app.UseHttpsRedirection()` comentado** en `RentaFacil.API/Program.cs` — es intencional para permitir que el celular se conecte por HTTP plano en la LAN durante Fase 1. No "arreglar" esto sin confirmar con el usuario; sí hay que revisarlo antes de exponer la API a internet (Fase 2).
- **El seed de datos dummy en `Program.cs`** (un Inquilino/Inmueble/Unidad/Contrato/Pago de ejemplo si la tabla está vacía) no es un bug ni datos de prueba olvidados — está puesto a propósito para tener algo que ver al correr contra una base nueva.
