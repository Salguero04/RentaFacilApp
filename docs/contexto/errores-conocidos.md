# Errores conocidos (gotchas)

> Trampas confirmadas leyendo el código real, no suposiciones. Si algo no se pudo confirmar, queda marcado `[PENDIENTE]` en vez de afirmado a ciegas.

## Cualquier usuario puede leer/editar/borrar datos de cualquier otro usuario (IDOR/BOLA)
- **Pasa cuando:** se llama a cualquier `GetAllAsync`/`GetByIdAsync`/`Update`/`Delete` de Inquilino, Inmueble, Contrato, Pago o Unidad.
- **Causa real:** todas las entidades tienen `UsuarioId`, pero ningún Repository/Service/Controller lo usa para filtrar — se asigna al crear, pero nunca se valida al leer o modificar. Confirmado leyendo `InquilinoService`, `InmuebleService`, `OtherServices.cs` y sus Controllers.
- **Solución:** pendiente de implementar — ver la sección "Pendiente" en `CLAUDE.md` y el punto 5 de `ClaudeCampeonatoatp.md` (es la prioridad #1 sugerida).

## El login de la app no protege nada en el servidor
- **Pasa cuando:** se asume que por tener `Login.razor`/`AuthService` la API está protegida.
- **Causa real:** `AuthService.cs` en MAUI valida contra `Preferences` local del dispositivo (usuario hardcodeado `admin/admin`); nunca llama a la API. La API no tiene `[Authorize]` en ningún Controller — cualquiera con la URL puede llamar los endpoints directo (con o sin pasar por el login de la app).
- **Solución:** pendiente — ver punto 2 de `ClaudeCampeonatoatp.md`.

## `UnidadesController` rompe el patrón de capas sin avisar
- **Pasa cuando:** se usa `UnidadesController` (en `OtherControllers.cs`) como referencia para escribir un Controller nuevo.
- **Causa real:** es el único Controller que inyecta `AppDbContext` directo en vez de un Service/Repository — parece un atajo de desarrollo, no hay justificación documentada.
- **Solución:** no copiar este patrón en código nuevo; si se tiene tiempo, refactorizarlo a Repository/Service como el resto.

## La IP de producción está hardcodeada en el código del cliente
- **Pasa cuando:** la IP de la LAN/servidor cambia y la app sigue apuntando a la vieja en builds Release.
- **Causa real:** `RentaFacil.MAUI/Config/ApiConfig.cs` tiene la URL de producción escrita literal en el código (`http://200.126.17.232:5295`), seleccionada vía el compile constant `LOCAL` (definido solo en Debug).
- **Solución:** al cambiar de red/servidor, actualizar esa línea y recompilar — no hay configuración en runtime todavía.

## `rentafacil.db` está versionado en git y cambia solo con `dotnet run`
- **Pasa cuando:** se corre la API localmente y luego se revisa `git status` — aparece `rentafacil.db` modificado aunque no se haya tocado código.
- **Causa real:** el archivo SQLite vive dentro de `RentaFacil.API/` y está trackeado en git (no en `.gitignore`); cada `dotnet run` aplica migraciones y/o el seed de datos dummy si la tabla `Inquilinos` está vacía, lo que modifica el archivo binario.
- **Solución:** no commitear este archivo por accidente junto con cambios de código — revisar el diff antes de `git add`. [PENDIENTE: confirmar con el usuario si se quiere agregar `*.db` a `.gitignore` y dejar de versionarlo, o si es intencional para tener datos de ejemplo compartidos.]

## Cosas que parecen rotas pero son a propósito
- **CORS abierto (`AllowAnyOrigin/Method/Header`) y `app.UseHttpsRedirection()` comentado** en `RentaFacil.API/Program.cs` — es intencional para permitir que el celular se conecte por HTTP plano en la LAN durante Fase 1. No "arreglar" esto sin confirmar con el usuario; sí hay que revisarlo antes de exponer la API a internet (Fase 2).
- **El seed de datos dummy en `Program.cs`** (un Inquilino/Inmueble/Unidad/Contrato/Pago de ejemplo si la tabla está vacía) no es un bug ni datos de prueba olvidados — está puesto a propósito para tener algo que ver al correr contra una base nueva.
