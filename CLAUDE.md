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

**Fecha:** 2026-06-28
**Plan ejecutado:** `C:\Users\msalg\.claude\plans\dreamy-mixing-pond.md` — **Servicios / Medidores (agua, luz) con reporte de pérdida**. Ejecutado directo sobre `main`. Verificado end-to-end en Chrome (web `localhost:5213`).

**Antecedentes (ya en `main`):** cliente **web Blazor WASM** (`RentaFacil.Web`) + UI compartida en la RCL `RentaFacil.UI` (6 proyectos), seguridad/auditoría (JWT+BCrypt, IDOR/BOLA cerrado, auditoría automática, rate limiting), globalización (`InvariantCulture`+`MoneyFormatter`+`.resx`), migración SQL Server con 4 schemas. Detalle en el historial de git. Las abstracciones de plataforma (`ITokenStore`/`IDispositivoServicio`) y el patrón de hosts (MAUI/Web) siguen vigentes.

**Esta tarea — servicios incluidos en el contrato + módulo Medidores:**
1. **Modelo (schema `renta`, migración `ServiciosMedidores`):** 3 entidades nuevas — `ServicioContrato` (hijo de Contrato, CASCADE: `Tipo`/`Modalidad`/`MontoFijo`), `CostoServicio` (hijo de Inmueble, CASCADE: la planilla real por `Inmueble+Tipo+Mes+Año`), `DetalleServicioPago` (hijo de Pago, CASCADE: desglose de lo cobrado). Enums nuevos `TipoServicio {Agua,Luz,Otro}` y `ModalidadServicio {MontoFijo,PorConsumo}` en `RentaFacil.Shared/Enums`. Gas excluido a propósito (bombona personal en Ecuador). `Pago.Servicios` se conserva como suma (compat recibo/Ingresos).
2. **Dos modalidades (decisión del usuario, ver `decisiones.md`):** `MontoFijo` = compartido (agua): inquilino paga fijo, el arrendador registra la planilla real y **la app reporta la diferencia como pérdida/ganancia**. `PorConsumo` = individual (luz): inquilino paga su consumo capturado al pagar, pass-through, sin pérdida.
3. **API:** DTOs en `RentaFacil.Shared/Models/ServicioDtos.cs` (+ `CrearContratoDto`/`ContratoDto`/`CrearPagoDto`/`PagoDto` extendidos con params opcionales para no romper call sites). `ContratoService`/`PagoService` (en `OtherServices.cs`) persisten y mapean los hijos; repos `Other*` hacen `Include`. Nuevo `CostoServicioService`/`CostoServicioRepository` + `ServiciosController` (`api/servicios/costos` CRUD upsert, `api/servicios/resumen?mes&anio`). El cálculo central de pérdida vive en `CostoServicioService.CalcularResumenAsync` (Σ montos fijos del inmueble − planilla real). Tests en `CostoServicioServiceTests.cs`.
4. **UI (`RentaFacil.UI/Pages`):** `CrearContrato` tiene editor "Servicios incluidos" (lista add/quitar, Total mensual en vivo). `CrearPago` carga los servicios del contrato (fijos prellenados, consumo capturado), `Total = renta + servicios`, guarda `DetalleServicioPago[]`. `Ingresos` tiene sección **"Medidores / Servicios"**: Cobrado / Planilla / Neto con pérdida en rojo + input para registrar la planilla del mes. `ReciboService` itemiza los servicios por tipo.

**Verificación hecha:** `dotnet build` limpio de `RentaFacil.UI` y `RentaFacil.API`; **59/59 tests verdes** (53 previos + 6 del cálculo de pérdida); migración aplicada en la BD al arrancar (3 tablas en `renta`). E2E en Chrome: contrato con agua-fijo $15 + luz-consumo → pago muestra Total $545 (500+15+30) → en Ingresos, planilla de agua $25 reporta **pérdida −$10** y la luz por consumo queda como pass-through $30 sin pérdida. **No verificado:** MAUI (no se rebuildeó esta tarea; la UI es compartida y debería funcionar, pero no se clickeó en MAUI); editar servicios de un contrato existente (no hay ruta de edición de contrato en la UI, solo creación).

**Gotchas de esta tarea:** (a) los DTOs de lectura/creación son `record` posicionales — los params nuevos van **al final con default** (`= null`) para no romper tests ni call sites. (b) `_Imports.razor` de `RentaFacil.UI` **no** incluye `@using RentaFacil.Shared.Enums`; cada `.razor` que use los enums debe declararlo (lo hacen `CrearContrato`, `CrearPago`, `Ingresos`). (c) la planilla compartida es a nivel **inmueble completo** (no subgrupos de unidades) — asunción V1. (d) reiniciar la API invalida el JWT en localStorage → re-login en la web.

**Próximo paso sugerido:** decidir si commitear (rama/PR). Posibles extensiones: gráficas en Ingresos, planilla por subgrupos de unidades, verificar en MAUI Android.

**Cuidado con procesos huérfanos:** `dotnet run` en background puede dejar el `.dll`/`.exe` bloqueados. API en puerto 5295, web en 5213. Antes de rebuildear: `netstat -ano | grep <puerto>` (Bash tool) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`.
