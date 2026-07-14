# Arquitectura

## En una frase
RentaFácil es una app con dos perfiles de cuenta — el **arrendador** (roles `Administrador`/`Propietario`: registra inquilinos, inmuebles/unidades, contratos y pagos, emite recibos PDF) y el **inquilino** (rol `Inquilino`: portal `/mi` de solo-su-data, se registra con el código QR que genera su arrendador y puede reportar pagos) — clientes MAUI Blazor Hybrid (móvil/escritorio) **y** web Blazor WebAssembly (navegador) que comparten una sola UI, + backend ASP.NET Core Web API.

## Stack
- Lenguaje / runtime: C# / .NET 10 (`net10.0` en los 6 proyectos)
- Framework principal: ASP.NET Core Web API (backend) + .NET MAUI Blazor Hybrid (cliente Android/iOS/Windows/MacCatalyst) + Blazor WebAssembly (cliente web en el navegador)
- UI compartida: las páginas `.razor`, layouts y `ApiClient` viven **una sola vez** en `RentaFacil.UI` (Razor Class Library); MAUI y Web la referencian. Lo único duplicado son 3 comportamientos de plataforma detrás de interfaces (`ITokenStore`, `IDispositivoServicio` en `RentaFacil.UI/Abstractions/`): guardar token (SecureStorage vs localStorage), abrir enlace (Launcher vs window.open), compartir/descargar PDF (Share nativo vs descarga del navegador).
- ORM: Entity Framework Core 10
- Base de datos: SQL Server (local + producción) con 4 schemas organizacionales fijos (`auth`/`renta`/`config`/`audit`) — ver `decisiones.md`. Connection string por máquina vía user-secrets (`ConnectionStrings:Default`). (SQLite y MySQL quedaron descartados.)
- PDF: QuestPDF (licencia Community) para recibos formato Ticket (80mm) y Carta (A4)
- Globalización: API y MAUI fuerzan `InvariantCulture`/`es-EC` al arrancar; `MoneyFormatter` (en `RentaFacil.Shared/Globalization/`) centraliza el formato de dinero (es-EC, `$X.XXX,XX`); infraestructura `.resx` lista para multiidioma (solo español poblado hoy) — ver `decisiones.md`.
- Tests: xUnit + Moq + FluentAssertions (`RentaFacil.Tests`)
- Tiempo real: SignalR (hub `/hubs/datos` en la API, `[Authorize]`, JWT por query string solo en paths `/hubs/*`; `IDataChangeNotifier` best-effort emite `"CambioDatos"(entidad, usuarioId, accion)` al mutar Pago/Contrato/ReportePago; desde 2026-07-14 los eventos van a **grupos por usuario** `usuario-{id}` — no a `Clients.All` — para aislar arrendadores e inquilinos; `SignalRClient` compartido en `RentaFacil.UI/Services`, suscrito en `Pagos.razor` y `ReportesPago.razor`).
- QR: la API genera el PNG del código de vinculación con **QRCoder** (servido autenticado); el cliente MAUI escanea con **ZXing.Net.Maui** (permiso CAMERA, abstracción `IEscanerQr` en `RentaFacil.UI/Abstractions/` con impl no-soportada en Web → allí se escribe el código a mano).
- Servicios externos: login con Google OAuth 2.0 tiene los **cimientos** implementados (2026-07-07: validación de ID token con `Google.Apis.Auth` en la API, endpoint `POST api/auth/login-google`, abstracción `IProveedorGoogle` con botón oculto en la UI) pero está **inactivo hasta configurar credenciales** (user-secrets `Google:ClientId`, opcional `Google:PermitirRegistro`) e implementar `IProveedorGoogle` real por plataforma; WhatsApp deep link sigue sin implementar — ver la sección "Pendiente" de `CLAUDE.md`.

## Mapa de carpetas
- `RentaFacil.Shared/` → DTOs (`Models/*Dto.cs`, records), enums (`Enums/TipoInmueble.cs`, `Enums/FrecuenciaPago.cs`) y `Globalization/MoneyFormatter`, compartidos por API y clientes. Sin lógica de negocio.
- `RentaFacil.UI/` → **Razor Class Library con la UI compartida entre MAUI y Web.** `Pages/*.razor` (todas las pantallas), `Layout/*` (MainLayout/NavMenu/LoginLayout), `Services/` (`ApiClient`, `AuthService`, `AuthHeaderHandler`), `ViewModels/EstadoInquilinoViewModel.cs`, `Abstractions/` (`ITokenStore`, `IDispositivoServicio` — las implementa cada host), y `_Marker.cs` (clase marcador para `Router.AdditionalAssemblies`). No registra DI ni define la URL de la API; eso lo hace cada host.
- `RentaFacil.API/Models/` → entidades EF Core (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`, `Recordatorio`, `Medidor`, `MedidorInquilino`, `FacturaMedidor`, `DetalleServicioPago`, `NotificacionPendiente`, `CodigoVinculacion`, `ReportePago`)
- `RentaFacil.API/Data/` → solo `AppDbContext.cs`
- `RentaFacil.API/Migrations/` → migraciones EF Core (en la raíz del proyecto, NO dentro de `Data/` — ojo: los docs de plan dibujan `Data/Migrations/`, pero el código real las tiene aquí)
- `RentaFacil.API/Repositories/` → acceso a datos, un repo + interfaz por entidad (`IInquilinoRepository`, etc.); `OtherRepositories.cs`/`IOtherRepositories.cs` agrupan Contrato/Pago
- `RentaFacil.API/Services/` → lógica de negocio (`InquilinoService`, `InmuebleService`, `ReciboService`, y `OtherServices.cs` para Contrato/Pago)
- `RentaFacil.API/Controllers/` → endpoints REST (`InquilinosController`, `InmueblesController`, `OtherControllers.cs` con `ContratosController`/`PagosController`/`UnidadesController`)
- `RentaFacil.API/Program.cs` → DI, CORS, migración automática al iniciar, seed de datos dummy
- `RentaFacil.MAUI/` → host MAUI Blazor Hybrid. Ya **no** contiene las pantallas (viven en `RentaFacil.UI`); aporta `Components/Routes.razor` (Router con `AdditionalAssemblies` a la RCL), `Platform/` (impls `MauiTokenStore`/`MauiDispositivoServicio`), `Config/ApiConfig.cs` (URL base de la API, distinta en Debug/Release), `MauiProgram.cs` (DI).
- `RentaFacil.Web/` → host Blazor WebAssembly. `App.razor` (Router → RCL), `Program.cs` (DI + URL de la API), `Platform/` (impls `WebTokenStore` con localStorage / `WebDispositivoServicio` con `window.open`+descarga), `wwwroot/` (Bootstrap + `app.css` tema + `appInterop.js` para descargar PDFs).
- `RentaFacil.Tests/` → tests de Services con Repository mockeado
- `betas APKs/` → APKs compilados de prueba
- `docs/contexto/` → estos documentos de contexto

## Flujo de datos
Una pantalla Blazor (`RentaFacil.UI/Pages/*.razor`) llama a `ApiClient` (HttpClient cuyo `BaseAddress` lo configura cada host: MAUI vía `ApiConfig.BaseUrl`, Web vía `Program.cs`) → la request HTTP, con el Bearer token adjuntado por `AuthHeaderHandler`, llega a un Controller de `RentaFacil.API` → el Controller delega en un Service (lógica de negocio) → el Service usa un Repository (EF Core) → `AppDbContext` lee/escribe en SQL Server. La respuesta vuelve como un DTO de `RentaFacil.Shared` que la página Blazor renderiza directo (sin un ViewModel intermedio salvo `EstadoInquilinoViewModel`). La misma página corre idéntica en MAUI (WebView nativo) y en el navegador (WASM).

## Esquema de base de datos
Code-First con EF Core sobre SQL Server (local y producción), montos `decimal(18,2)`. Las tablas viven en 4 schemas organizacionales fijos (no por tenant): `auth` (Usuarios), `renta` (Inquilino/Inmueble/Unidad/Contrato/Pago/Recordatorio + Medidor/MedidorInquilino/FacturaMedidor/DetalleServicioPago/NotificacionPendiente, cada fila filtrada por `UsuarioId`), `config` (catálogos globales + `__EFMigrationsHistory`) y `audit` (hoy la auditoría vive como columnas `IAuditable` en `renta.*`). El esquema, las relaciones, los schemas y los índices de `UsuarioId` se definen en `AppDbContext.OnModelCreating` y se materializan en `RentaFacil.API/Migrations/` (migración `InitialSqlServer`, luego `ServiciosMedidores` y `MedidoresRediseno` — esta última dropea las tablas de `ServiciosMedidores` y las reemplaza, ver `decisiones.md`) — esa es la fuente de verdad del DDL. Para detalle campo por campo de cada entidad ver `glosario.md`.

```
Inquilinos ||--o{ Contratos           : posee      (FK InquilinoId, borrado RESTRICT)
Inquilinos ||--o{ Recordatorios       : recuerda    (FK InquilinoId, borrado CASCADE)
Inquilinos ||--o{ MedidoresInquilino  : se vincula  (FK InquilinoId, borrado RESTRICT)
Inmuebles  ||--o{ Unidades            : contiene    (FK InmuebleId,  borrado CASCADE)
Inmuebles  ||--o{ Medidores           : mide        (FK InmuebleId,  borrado CASCADE)
Unidades   ||--o{ Contratos           : alquila     (FK UnidadId,    borrado RESTRICT)
Contratos  ||--o{ Pagos               : recibe      (FK ContratoId,  borrado CASCADE)
Pagos      ||--o{ DetallesServicioPago : desglosa   (FK PagoId,      borrado CASCADE)
Medidores  ||--o{ MedidoresInquilino  : vincula     (FK MedidorId,   borrado CASCADE)
Medidores  ||--o{ FacturasMedidor     : planillas   (FK MedidorId,   borrado CASCADE)
```

Módulo inquilino (2026-07-14): `Inquilino.UsuarioCuentaId int?` referencia la cuenta `auth.Usuarios` del inquilino (null = aún sin registrarse; índice no único — una cuenta puede vincular varios `Inquilino` de distintos arrendadores). `CodigosVinculacion` (código único de 8 chars por contrato, expira 7 días, un solo uso con reclamo atómico) y `ReportesPago` (reporte del inquilino con estado Pendiente/Confirmado/Rechazado y `FotoComprobante varbinary` ≤1MB) viven en `renta` y referencian Contrato/Inquilino **sin FK estricta** (mismo criterio que `Recordatorio.ContratoId`).

Reglas de borrado (definidas en `OnModelCreating`):
- Borrar un **Inmueble** elimina en cascada sus **Unidades** y sus **Medidores**.
- Borrar un **Contrato** elimina en cascada sus **Pagos**. `MedidorInquilino.ContratoId` es informativo (sin FK estricta), igual que `Recordatorio.ContratoId`.
- Borrar un **Pago** elimina en cascada sus **DetallesServicioPago**.
- Borrar un **Medidor** elimina en cascada sus **MedidoresInquilino** (vínculos) y **FacturasMedidor** (planillas).
- NO se puede borrar un **Inquilino** con Contratos ni una **Unidad** con Contratos (RESTRICT) — primero hay que quitar/cerrar los contratos. Tampoco un **Inquilino** con `MedidorInquilino` activos (RESTRICT).
- `Inmueble.MontoRenta` solo aplica a `Tipo == Unico`; en `Multiple` la renta vive en cada `Unidad`.
- `UsuarioId` está en las 11 entidades de `renta.*` (Inquilino/Inmueble/Unidad/Contrato/Pago/Recordatorio/Medidor/MedidorInquilino/FacturaMedidor/DetalleServicioPago/NotificacionPendiente) y **sí** se usa para filtrar cada lectura/escritura en los Repositories (IDOR/BOLA cerrado, indexado) — ver `errores-conocidos.md`. (`DetalleServicioPago` siempre se accede vía su `Pago` padre, ya filtrado por `UsuarioId`.)

## Lo que NO existe (y no hay que crear sin que lo pidan)
- No hay caché de ningún tipo.
- No hay Docker en uso real (está en el plan para Fase 2, pero no hay `Dockerfile`/`docker-compose.yml` en el repo).
- No hay paginación en los `GetAll()` de la API.
- No hay CI/CD configurado (no hay workflows en `.github/workflows/`).
- No hay multiusuario de *arrendadores*: el registro de cuentas arrendador vía `/api/auth/registrar` exige ser Administrador. (El rol `Inquilino` SÍ tiene registro self-service, pero solo con un código de vinculación vigente — `/api/auth/registrar-inquilino`, 2026-07-14.)

(Autenticación JWT, filtrado por `UsuarioId`, auditoría de cambios, y el módulo/portal del inquilino — que antes estaban en esta lista o en "Pendiente" — ya están implementados; ver el resto de este documento y `decisiones.md`.)
