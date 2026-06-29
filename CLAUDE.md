# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio.

> **Este archivo es solo un índice.** Da mini-resúmenes y enlaza al detalle. NO leas todo: lee la sección que necesites y, si requieres más, abre el `.md` enlazado. Responder siempre en español.

## En una frase
RentaFácil: app personal (aún de un solo usuario) para que un arrendador gestione inquilinos, inmuebles/unidades, contratos de alquiler y pagos, con recibos en PDF. **Dos clientes que comparten una sola UI** (`RentaFacil.UI`, Razor Class Library): **.NET MAUI Blazor Hybrid** (móvil + escritorio) y **Blazor WebAssembly** (navegador). Backend **ASP.NET Core Web API** (.NET 10, EF Core, **SQL Server** local y producción con schemas `auth`/`renta`/`config`/`audit`). Código, comentarios y UI en español.

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
- Módulo Ingresos con **gráficas** (los servicios/medidores de agua/luz ya están implementados — ver "Último Contexto" 2026-06-28; falta solo la parte de gráficas/analítica).
- Futuro: suscripciones (Gratis/Pro), app iOS, dashboard web con gráficas/analítica (el cliente web base ya existe — `RentaFacil.Web`, Blazor WASM; falta el módulo de reportes/gráficas), firma digital en contratos.

## Último Contexto
> Sección de handoff: dónde quedó el trabajo y cómo continuar. **Reescribir** (no acumular histórico) tras cada cambio mediano/mayor.

**Fecha:** 2026-06-29
**Commit:** `c886e38` (rama `feature/medidores-rediseno`, mergeada a `main` en `06f7e56`) — **rediseño del módulo de Servicios/Medidores + edición de contratos**. Este commit **reemplaza** el modelo de servicios-en-contrato de la tarea anterior (2026-06-28: `ServicioContrato`/`CostoServicio`) por uno centrado en el medidor — ver `decisiones.md`. El commit también arrastró sin commitear de sesiones previas el cliente **web Blazor WASM** (`RentaFacil.Web`) y la RCL `RentaFacil.UI` con la UI compartida.

**Antecedentes (ya en `main`):** 6 proyectos (`Shared`/`API`/`UI`/`MAUI`/`Web`/`Tests`), seguridad/auditoría (JWT+BCrypt, IDOR/BOLA cerrado, auditoría automática, rate limiting), globalización (`InvariantCulture`+`MoneyFormatter`+`.resx`), migración SQL Server con 4 schemas. Detalle en el historial de git. Las abstracciones de plataforma (`ITokenStore`/`IDispositivoServicio`) y el patrón de hosts (MAUI/Web) siguen vigentes.

**Esta tarea — Medidores como entidad propia (reemplaza ServicioContrato/CostoServicio) + edición de contratos:**
1. **Modelo (schema `renta`, migración `MedidoresRediseno`, sobre la base de `ServiciosMedidores` del día anterior):** la migración **dropea** `ServiciosContrato` y `CostosServicio` y crea `Medidor` (por Inmueble, CASCADE: `Nombre`/`Tipo`/`Modo` `PorLectura`|`PorPlanilla`/`SubConsumoHabilitado`/`Tarifa`), `MedidorInquilino` (vínculo medidor↔inquilino con `ContratoId?` informativo, `MetodoCobro` `Tarifa`|`Prorrateo`|`Manual`, lecturas anterior/actual, `MontoFijo`), `FacturaMedidor` (planilla real por medidor+mes+año, upsert), y `NotificacionPendiente` (hook para una futura app del inquilino: hoy solo se persiste cuando se edita un contrato, no dispara push). `DetalleServicioPago` (hijo de Pago) se mantiene igual que antes. Enum `TipoServicio` ahora es `{Agua=0, Electricidad=1 (antes "Luz"), Gas=2, Otro=3}` — Gas sigue sin usarse en la práctica (bombona personal en Ecuador), pero ya existe en el enum.
2. **Tres métodos de cobro por vínculo (no dos modalidades como antes):** `Tarifa` = `(LecturaActual − LecturaAnterior) × Medidor.Tarifa`. `Prorrateo` = reparte la `FacturaMedidor` del periodo entre los vínculos en `Prorrateo` de ese medidor, proporcional al consumo (o partes iguales si no hay lecturas) — esto reemplaza la pérdida/ganancia de la modalidad `MontoFijo` anterior, pero ahora calculada por medidor y prorrateada entre todos los inquilinos vinculados, no por inmueble completo. `Manual` = monto fijo ingresado por el arrendador. Cálculo central en `MedidorService.CalcularCobros` (privado, estático).
3. **API:** `MedidoresController` (`api/medidores`, archivo **`ServiciosController.cs`** — el nombre del archivo no se actualizó al renombrar la clase, ver nota abajo) con endpoints CRUD de medidores, vínculos (`/inquilinos`), facturas/planillas (`/facturas`), `GET /resumen?mes&anio` (reporte Ingresos: cobrado/planilla/neto por medidor) y `GET /cobros?contratoId&mes&anio` (lo que usa `CrearPago` para precargar servicios). `MedidorService`/`IMedidorService` + repos `MedidorRepositories.cs`/`IMedidorRepositories.cs`. `ContratoService.UpdateAsync` ahora existe (antes no había edición) y registra un `NotificacionPendiente` al guardar cambios.
4. **UI (`RentaFacil.UI/Pages`):** página nueva `Medidores.razor` (`/medidores`, con entrada en el menú) para listar/configurar medidores, vincular inquilinos y registrar facturas. `CrearContrato.razor` ahora maneja también edición (`@page "/contratos/editar/{Id:int}"`, antes solo `/contratos/nuevo`). `CrearPago` calcula los servicios a cobrar consultando `api/medidores/cobros` (ya no hay editor de "servicios incluidos" en el contrato). `Ingresos.razor` lee del nuevo `api/medidores/resumen`.
5. **Tests:** `MedidorServiceTests.cs` (reemplaza/extiende los tests del diseño anterior) cubre los 3 métodos de cobro + el cálculo de pérdida vía prorrateo. **59/59 tests verdes.**

**Verificación hecha (según el mensaje de commit):** 59/59 tests, migración `MedidoresRediseno` aplicada. **No verificado en esta sesión de pull:** no se corrió build/tests localmente tras el `git pull` — pendiente confirmar que todo sigue verde en esta máquina.

**Gotchas de esta tarea:** (a) `RentaFacil.API/Controllers/ServiciosController.cs` contiene la clase `MedidoresController` — desalineación de nombre archivo↔clase, no es bug funcional pero confunde si se busca por nombre de archivo. (b) `MedidorInquilino.ContratoId` es informativo (sin FK estricta), igual que `Recordatorio.ContratoId`. (c) `NotificacionPendiente` no dispara nada todavía (ni push ni email) — solo queda persistida como hook a futuro. (d) la planilla (`FacturaMedidor`) y el prorrateo son por **medidor**, no por inmueble completo — distinto del diseño anterior (2026-06-28) que era por inmueble+tipo.

**Próximo paso sugerido:** correr `dotnet build`/`dotnet test` en esta máquina para confirmar que el pull no rompió nada localmente; decidir si vale la pena renombrar `ServiciosController.cs` → `MedidoresController.cs`; revisar la rama remota `feature/medidores-rediseno` (¿borrarla ya que se mergeó?).

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. API en puerto 5295, web en 5213. Antes de rebuildear: `netstat -ano | grep <puerto>` (Bash tool) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`.
