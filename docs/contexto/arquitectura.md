# Arquitectura

## En una frase
RentaFácil es una app personal (no multiusuario todavía) para que un arrendador registre inquilinos, inmuebles/unidades, contratos de alquiler y pagos, y emita recibos en PDF — clientes MAUI Blazor Hybrid (móvil/escritorio) **y** web Blazor WebAssembly (navegador) que comparten una sola UI, + backend ASP.NET Core Web API.

## Stack
- Lenguaje / runtime: C# / .NET 10 (`net10.0` en los 6 proyectos)
- Framework principal: ASP.NET Core Web API (backend) + .NET MAUI Blazor Hybrid (cliente Android/iOS/Windows/MacCatalyst) + Blazor WebAssembly (cliente web en el navegador)
- UI compartida: las páginas `.razor`, layouts y `ApiClient` viven **una sola vez** en `RentaFacil.UI` (Razor Class Library); MAUI y Web la referencian. Lo único duplicado son 3 comportamientos de plataforma detrás de interfaces (`ITokenStore`, `IDispositivoServicio` en `RentaFacil.UI/Abstractions/`): guardar token (SecureStorage vs localStorage), abrir enlace (Launcher vs window.open), compartir/descargar PDF (Share nativo vs descarga del navegador).
- ORM: Entity Framework Core 10
- Base de datos: SQL Server (local + producción) con 4 schemas organizacionales fijos (`auth`/`renta`/`config`/`audit`) — ver `decisiones.md`. Connection string por máquina vía user-secrets (`ConnectionStrings:Default`). (SQLite y MySQL quedaron descartados.)
- PDF: QuestPDF (licencia Community) para recibos formato Ticket (80mm) y Carta (A4)
- Globalización: API y MAUI fuerzan `InvariantCulture`/`es-EC` al arrancar; `MoneyFormatter` (en `RentaFacil.Shared/Globalization/`) centraliza el formato de dinero (es-EC, `$X.XXX,XX`); infraestructura `.resx` lista para multiidioma (solo español poblado hoy) — ver `decisiones.md`.
- Tests: xUnit + Moq + FluentAssertions (`RentaFacil.Tests`)
- Servicios externos: ninguno integrado todavía (OAuth de Google, WhatsApp deep link, etc. están planeados pero no implementados — ver la sección "Pendiente" de `CLAUDE.md`)

## Mapa de carpetas
- `RentaFacil.Shared/` → DTOs (`Models/*Dto.cs`, records), enums (`Enums/TipoInmueble.cs`, `Enums/FrecuenciaPago.cs`) y `Globalization/MoneyFormatter`, compartidos por API y clientes. Sin lógica de negocio.
- `RentaFacil.UI/` → **Razor Class Library con la UI compartida entre MAUI y Web.** `Pages/*.razor` (todas las pantallas), `Layout/*` (MainLayout/NavMenu/LoginLayout), `Services/` (`ApiClient`, `AuthService`, `AuthHeaderHandler`), `ViewModels/EstadoInquilinoViewModel.cs`, `Abstractions/` (`ITokenStore`, `IDispositivoServicio` — las implementa cada host), y `_Marker.cs` (clase marcador para `Router.AdditionalAssemblies`). No registra DI ni define la URL de la API; eso lo hace cada host.
- `RentaFacil.API/Models/` → entidades EF Core (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`)
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
Code-First con EF Core sobre SQL Server (local y producción), montos `decimal(18,2)`. Las tablas viven en 4 schemas organizacionales fijos (no por tenant): `auth` (Usuarios), `renta` (Inquilino/Inmueble/Unidad/Contrato/Pago + ServicioContrato/CostoServicio/DetalleServicioPago, cada fila filtrada por `UsuarioId`), `config` (catálogos globales + `__EFMigrationsHistory`) y `audit` (hoy la auditoría vive como columnas `IAuditable` en `renta.*`). El esquema, las relaciones, los schemas y los índices de `UsuarioId` se definen en `AppDbContext.OnModelCreating` y se materializan en `RentaFacil.API/Migrations/` (migración `InitialSqlServer`, luego `ServiciosMedidores`) — esa es la fuente de verdad del DDL. Para detalle campo por campo de cada entidad ver `glosario.md`.

```
Inquilinos ||--o{ Contratos          : posee     (FK InquilinoId, borrado RESTRICT)
Inmuebles  ||--o{ Unidades           : contiene  (FK InmuebleId,  borrado CASCADE)
Inmuebles  ||--o{ CostosServicio     : planillas (FK InmuebleId,  borrado CASCADE)
Unidades   ||--o{ Contratos          : alquila   (FK UnidadId,    borrado RESTRICT)
Contratos  ||--o{ Pagos              : recibe    (FK ContratoId,  borrado CASCADE)
Contratos  ||--o{ ServiciosContrato  : incluye   (FK ContratoId,  borrado CASCADE)
Pagos      ||--o{ DetallesServicioPago : desglosa (FK PagoId,     borrado CASCADE)
```

Reglas de borrado (definidas en `OnModelCreating`):
- Borrar un **Inmueble** elimina en cascada sus **Unidades** y sus **CostosServicio** (planillas).
- Borrar un **Contrato** elimina en cascada sus **Pagos** y sus **ServiciosContrato**.
- Borrar un **Pago** elimina en cascada sus **DetallesServicioPago**.
- NO se puede borrar un **Inquilino** con Contratos ni una **Unidad** con Contratos (RESTRICT) — primero hay que quitar/cerrar los contratos.
- `Inmueble.MontoRenta` solo aplica a `Tipo == Unico`; en `Multiple` la renta vive en cada `Unidad`.
- `UsuarioId` está en las 8 entidades de `renta.*` y **sí** se usa para filtrar cada lectura/escritura en los Repositories (IDOR/BOLA cerrado, indexado) — ver `errores-conocidos.md`. (`DetalleServicioPago` y `ServicioContrato` siempre se acceden vía su padre, ya filtrado por `UsuarioId`.)

## Lo que NO existe (y no hay que crear sin que lo pidan)
- No hay caché de ningún tipo.
- No hay Docker en uso real (está en el plan para Fase 2, pero no hay `Dockerfile`/`docker-compose.yml` en el repo).
- No hay paginación en los `GetAll()` de la API.
- No hay CI/CD configurado (no hay workflows en `.github/workflows/`).
- No hay multiusuario real más allá de roles básicos (`Administrador`/`Propietario`) — el registro de cuentas vía `/api/auth/registrar` exige ya estar autenticado, no es self-service público.

(Autenticación JWT, filtrado por `UsuarioId`, y auditoría de cambios — que antes estaban en esta lista — ya están implementados; ver el resto de este documento y `decisiones.md`.)
