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
**Plan ejecutado:** `docs/superpowers/plans/2026-06-26-globalizacion.md` (9 tasks), ejecución inline (no subagentes) en la rama `feature/globalizacion` (creada desde `main`). **NO se ha mergeado a `main` todavía** — pendiente de decisión del usuario (siguiente paso: `superpowers:finishing-a-development-branch`).

**Antecedentes (ya en `main`):**
- Plan de seguridad/auditoría (20 tasks): autenticación JWT+BCrypt, IDOR/BOLA cerrado en las 5 entidades, auditoría automática, cabeceras de seguridad HTTP, rate limiting, validación de cédula/RUC. Puntos 1-5 de `ClaudeCampeonatoatp.md` cubiertos (también actualizado para reflejarlo).
- Plan de migración SQL Server (9 tasks): SQLite/MySQL reemplazados por SQL Server con 4 schemas (`auth`/`renta`/`config`/`audit`), `decimal(18,2)`, `IDbContextFactory` para futuro BD-por-tenant. Pendiente del usuario: aplicar la migración en la máquina de casa (`DESKTOP-07M16LE\LOCALDB#9246A1FB`).

**Globalización — Tasks 1-9 COMPLETAS y commiteadas en `feature/globalizacion`.** Objetivo: evitar bugs de punto/coma decimal y dejar infraestructura lista para multiidioma. **Corrección importante hecha antes de ejecutar:** el plan original tenía nombres de campo inventados (`Contrato.MontoMensual`, `Pago.Monto`) que no existen en el código — se corrigieron a los reales (`Contrato.Monto`/`Garantia`, `Pago.TotalMonto`/`ACuenta`/`Servicios`) antes de escribir el plan final. 50 tests verdes (13 nuevos de `MoneyFormatterTests`). Detalle por task:
1. `InvariantCulture` fijado al arrancar `Program.cs` (antes de `WebApplication.CreateBuilder`). Verificado: login y `/api/inquilinos` siguen funcionando.
2. Verificación (sin cambio de código): los DTOs ya usaban `decimal` puro para dinero, nunca `string`.
3. `MoneyFormatter` (`RentaFacil.Shared/Globalization/MoneyFormatter.cs`): `Mostrar`/`MostrarNumero`/`Parsear`, cultura es-EC por defecto. **Gotcha de xUnit:** `[InlineData]` con literal numérico para un parámetro `decimal?` falla en runtime (`double` no convierte a `decimal?` por reflexión en `CheckValue`, aunque sí convierte a `decimal` no-nullable) — se resolvió recibiendo el parámetro como `object?` y convirtiendo con `Convert.ToDecimal` dentro del test.
4. `AddLocalization` + `UseRequestLocalization` (es-EC default, es, en-US) en la API después de auth/authz, antes de `MapControllers`. En MAUI: `AddLocalization` + cultura fijada a es-EC en `App.xaml.cs`. **Gotcha:** el paquete `Microsoft.Extensions.Localization` no viene por defecto en el SDK de MAUI (sí en el `Sdk.Web` de la API) — hubo que agregarlo explícitamente al `.csproj`.
5. Infraestructura `.resx` en `RentaFacil.Shared/Globalization/Resources/`: `SharedResources.cs` (clase marcador) + `SharedResources.es.resx` (poblado: moneda, errores de formato/identificación/campo requerido) + `SharedResources.en.resx` (vacío, listo para cuando se traduzca). Genera satellite assemblies `es`/`en` correctamente.
6. MAUI — reemplazado `$@valor`/`.ToString("F2")` por `MoneyFormatter.Mostrar()` en **7 archivos** (el plan original solo anticipaba 4: `Contratos.razor`, `Pagos.razor`, `DetallePagos.razor`, `Unidades.razor` — el grep completo encontró 3 más: `CrearPago.razor`, `Home.razor`, `Ingresos.razor`). **No se tocaron los inputs** (`type="number"`/`InputNumber` en `CrearContrato`/`CrearPago`/`CrearInmueble`) — ya son culture-safe por diseño de Blazor (HTML5 number siempre parsea con punto decimal internamente, sin importar la cultura del dispositivo); migrarlos a `type="text"` con parseo manual (como sugería el borrador del plan) habría sido una regresión.
7. `ReciboService.cs` (QuestPDF): los 4 montos del recibo + la fecha ahora usan `MoneyFormatter.Mostrar()` y `ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)` explícito en vez de `$"${pago.X}"`/`{pago.FechaPago:dd/MM/yyyy}` (dependían de la `CurrentCulture` del hilo del servidor). Verificado generando un recibo PDF real: muestra `$500,00`.
8. Tests de `MoneyFormatter` (13, ver Task 3) cubren el objetivo de esta task — no se duplicó en un archivo separado.
9. Verificación final: build de API y MAUI Android limpios, 50/50 tests verdes, recibo PDF real con formato correcto. Docs actualizados: `docs/contexto/arquitectura.md` (sección Stack) y `docs/contexto/decisiones.md` (nueva decisión `InvariantCulture`+`MoneyFormatter`+`.resx`).

**Próximo paso sugerido:** invocar `superpowers:finishing-a-development-branch` para decidir merge/PR/cleanup de `feature/globalizacion`. Tras eso, el usuario indicó que el foco pasa a **lógica y UX/UI** (sin spec todavía — usar `superpowers:brainstorming` cuando se aborde, no implementar directo).

**Cuidado con procesos huérfanos:** durante esta sesión, `dotnet run` en background dejó el `.dll`/`.exe` bloqueados varias veces. Antes de rebuildear: `netstat -ano | grep 5295` (Bash tool, Git Bash) para hallar el PID en `LISTENING`, luego `taskkill //PID <pid> //F`. Preferir `timeout 20 dotnet run --no-build` en vez de `(dotnet run &)` suelto para que se auto-mate solo.
