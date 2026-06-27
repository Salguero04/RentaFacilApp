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
- Notificaciones automáticas/push de vencimiento de pago (los Recordatorios manuales —nota + fecha, sin push— ya están implementados, ver "Último Contexto" 2026-06-27).
- Deep link de WhatsApp al compartir recibo: `Home.razor.CompartirWhatsApp` hoy usa el share genérico de MAUI (`DataTransfer.Share`, el usuario elige la app destino), no abre WhatsApp directo con `wa.me`/`whatsapp://send`.
- Medidores de servicios (agua/luz) por unidad; módulo Ingresos con gráficas.
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web, firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-06-27
**Plan ejecutado:** `C:\Users\masalgue\.claude\plans\voy-comenzar-con-la-warm-charm.md` — auditoría de UI/UX leyendo 30 capturas de referencia de una app competidora ("Bacotich Alquileres") en `Imagenes de Referencia/`, ejecutado directo sobre `main` (sin branch nueva). **`feature/globalizacion` (sesión anterior) ya está mergeada a `main`** — la entrada previa de este documento decía "NO se ha mergeado todavía", quedó stale; los commits `b404fa2`..`b4f85f0` ya viven en `main`.

**Antecedentes (ya en `main`):** seguridad/auditoría (JWT+BCrypt, IDOR/BOLA cerrado, auditoría automática, rate limiting), globalización (`InvariantCulture`+`MoneyFormatter`+infraestructura `.resx`), y migración a SQL Server con 4 schemas — **ya verificada en ambas máquinas** (trabajo `GGCBOADMWRK025\SQLEXPRESS`, y casa `DESKTOP-07M16LE` vía `sqllocaldb`/`MSSQLLocalDB`; en casa hubo que instalar `dotnet-ef` global y crear los user-secrets desde cero por ser un clone nuevo — connection string estable: `Server=(localdb)\MSSQLLocalDB;...`). Detalle en entradas previas de este archivo (ver historial de git si se necesita el detalle completo, este archivo no acumula histórico).

**Esta tarea — bugs de lógica/datos + unificación de UI + Recordatorios persistidos:**
1. **Bug crítico de datos:** `EstadoInquilinoViewModel.HaPagado` (`PagoActual != null`) marcaba como pagado cualquier pago parcial — corregido a `PagoActual?.Completado ?? false`; `SaldoPendiente` ahora calcula `(TotalMonto + Servicios) - ACuenta` con `Math.Max(0, ...)` en vez de asumir 0 si "pagado".
2. `Facturado` ahora se persiste de punta a punta: se agregó a `CrearPagoDto`, `PagoService.CrearAsync/UpdateAsync` ya no hardcodea `false`, `CrearPago.razor` envía el checkbox "Entregar Factura", y `Home.razor.ToggleFactura()` ahora hace `PUT api/pagos/{id}` en vez de mutar solo en memoria (nuevo `ApiClient.ActualizarPagoAsync`). Si no existe `PagoActual` para el período, se oculta la opción "Entregar factura" del bottom-sheet (no hay registro al que adjuntar el flag).
3. `Frecuencia` de pago (`Contrato.Frecuencia`, existía en el modelo EF pero no en los DTOs) ahora se expone en `CrearContratoDto`/`ContratoDto` y se mapea en `ContratoService`; el `<select>` de `CrearContrato.razor` pasó de decorativo (sin `@bind`, con "Anual" que no existe en el enum) a bindeado contra `FrecuenciaPago` real (Mensual/Quincenal/Semanal). También: el monto se autocompleta con `Unidad.MontoRenta` al seleccionar unidad (antes se reseteaba a 0), y `GuardarContrato` tiene try/catch con `errorMessage` visible.
4. Manejo de errores visible (antes `Console.WriteLine` silencioso) agregado en `CrearInquilino.razor`, `CrearInmueble.razor`, `Contratos.razor`. Label de `Contratos.razor` corregido de "Total: X Activos" a "Total: X Contratos" (la lista nunca filtró por `Activo`). Timeout de `AuthService` subido de 5s a 20s. URL de recibo en `Pagos.razor` ahora se construye con `new Uri(...)` en vez de concatenación de string.
5. **Unificación de patrón visual:** `Unidades.razor` era la única pantalla con formulario inline (sin bottom-sheet, sin página separada). Se migró al mismo patrón que `Inquilinos.razor`/`Inmuebles.razor`: tarjetas con bottom-sheet de Editar/Eliminar + nueva página `CrearUnidad.razor` (`/inmuebles/{id}/unidades/nueva` y `/unidades/editar/{id}`) + FAB circular.
6. `Pagos.razor` (vista secundaria en tabla, ruta `/pagos`, sin entrada en el menú) ganó columna "Saldo" y badge Facturado/Sin Factura, igual que `DetallePagos.razor`.
7. **Recordatorios — nueva entidad persistida** (antes el bottom-sheet "Nuevo Recordatorio" de `Home.razor` era pura maqueta, sin `@bind` ni guardado): entidad `Recordatorio` (`InquilinoId` FK cascada, `ContratoId?` informativo, `Detalle`, `FechaProgramada`) en schema `renta`, migración `AddRecordatorios`, repo/service/controller siguiendo el patrón `Model→Repository→Service→Controller` existente, endpoints `GET/POST/DELETE api/recordatorios`. `Home.razor` ahora bindea el formulario y llama a la API al guardar. **No incluye** notificaciones push/automáticas — sigue siendo nota manual, ver "Pendiente".
8. Semántica de color de "Estado de Pagos" (`Home.razor.GetEstadoColor`) confirmada como **ya implementada correctamente** (coincide con las 30 capturas de referencia) — se quitó el `[PENDIENTE]` de `glosario.md` que pedía confirmar esto.

**Verificación hecha:** build limpio (API/Shared/Tests, MAUI Windows, MAUI Android por separado — la `.slnx` completa sigue fallando con `NETSDK1047` por el mismo motivo no relacionado que ya quedó documentado en la entrada de la máquina de casa), 53/53 tests verdes (3 nuevos), migración `AddRecordatorios` aplicada en SQL Server (tabla `renta.Recordatorios` creada con FK cascada). **No verificado:** correr la app MAUI en un emulador/dispositivo real para clickear las pantallas nuevas (Unidades, toggle de factura, recordatorio) — queda para el usuario o una próxima sesión.

**Próximo paso sugerido:** probar manualmente en la app real lo de arriba. El deep-link de WhatsApp al compartir recibo queda documentado como pendiente en la sección "Pendiente" (decisión explícita del usuario: no implementarlo en esta tarea).

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
