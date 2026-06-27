# Arquitectura

## En una frase
RentaFácil es una app personal (no multiusuario todavía) para que un arrendador registre inquilinos, inmuebles/unidades, contratos de alquiler y pagos, y emita recibos en PDF — cliente móvil/escritorio en MAUI Blazor Hybrid + backend ASP.NET Core Web API.

## Stack
- Lenguaje / runtime: C# / .NET 10 (`net10.0` en los 4 proyectos)
- Framework principal: ASP.NET Core Web API (backend) + .NET MAUI Blazor Hybrid (cliente Android/iOS/Windows/MacCatalyst)
- ORM: Entity Framework Core 10
- Base de datos: SQL Server (local + producción) con 4 schemas organizacionales fijos (`auth`/`renta`/`config`/`audit`) — ver `decisiones.md`. Connection string por máquina vía user-secrets (`ConnectionStrings:Default`). (SQLite y MySQL quedaron descartados.)
- PDF: QuestPDF (licencia Community) para recibos formato Ticket (80mm) y Carta (A4)
- Tests: xUnit + Moq + FluentAssertions (`RentaFacil.Tests`)
- Servicios externos: ninguno integrado todavía (OAuth de Google, WhatsApp deep link, etc. están planeados pero no implementados — ver la sección "Pendiente" de `CLAUDE.md`)

## Mapa de carpetas
- `RentaFacil.Shared/` → DTOs (`Models/*Dto.cs`, records) y enums (`Enums/TipoInmueble.cs`, `Enums/FrecuenciaPago.cs`) compartidos por API y cliente. Sin lógica.
- `RentaFacil.API/Models/` → entidades EF Core (`Inquilino`, `Inmueble`, `Unidad`, `Contrato`, `Pago`)
- `RentaFacil.API/Data/` → solo `AppDbContext.cs`
- `RentaFacil.API/Migrations/` → migraciones EF Core (en la raíz del proyecto, NO dentro de `Data/` — ojo: los docs de plan dibujan `Data/Migrations/`, pero el código real las tiene aquí)
- `RentaFacil.API/Repositories/` → acceso a datos, un repo + interfaz por entidad (`IInquilinoRepository`, etc.); `OtherRepositories.cs`/`IOtherRepositories.cs` agrupan Contrato/Pago
- `RentaFacil.API/Services/` → lógica de negocio (`InquilinoService`, `InmuebleService`, `ReciboService`, y `OtherServices.cs` para Contrato/Pago)
- `RentaFacil.API/Controllers/` → endpoints REST (`InquilinosController`, `InmueblesController`, `OtherControllers.cs` con `ContratosController`/`PagosController`/`UnidadesController`)
- `RentaFacil.API/Program.cs` → DI, CORS, migración automática al iniciar, seed de datos dummy
- `RentaFacil.MAUI/Components/Pages/*.razor` → pantallas (Inquilinos, Inmuebles, Unidades, Contratos, Pagos, Ingresos, Login, etc.)
- `RentaFacil.MAUI/Components/Layout/` → `MainLayout.razor`, `LoginLayout.razor`, `NavMenu.razor`
- `RentaFacil.MAUI/Services/` → `ApiClient.cs` (llamadas HTTP a la API), `AuthService.cs` (login local, ver `errores-conocidos.md`)
- `RentaFacil.MAUI/Config/ApiConfig.cs` → URL base de la API, distinta en Debug/Release
- `RentaFacil.Tests/` → tests de Services con Repository mockeado
- `betas APKs/` → APKs compilados de prueba
- `docs/contexto/` → estos documentos de contexto

## Flujo de datos
Una pantalla Blazor (`Components/Pages/*.razor`) llama a `Services/ApiClient.cs` (HttpClient apuntando a `ApiConfig.BaseUrl`) → la request HTTP llega a un Controller de `RentaFacil.API` → el Controller delega en un Service (lógica de negocio) → el Service usa un Repository (EF Core) → `AppDbContext` lee/escribe en SQL Server. La respuesta vuelve como un DTO de `RentaFacil.Shared` que la página Blazor renderiza directo (sin un ViewModel intermedio salvo `EstadoInquilinoViewModel`).

## Esquema de base de datos
Code-First con EF Core sobre SQL Server (local y producción), montos `decimal(18,2)`. Las tablas viven en 4 schemas organizacionales fijos (no por tenant): `auth` (Usuarios), `renta` (Inquilino/Inmueble/Unidad/Contrato/Pago, cada fila filtrada por `UsuarioId`), `config` (catálogos globales + `__EFMigrationsHistory`) y `audit` (hoy la auditoría vive como columnas `IAuditable` en `renta.*`). El esquema, las relaciones, los schemas y los índices de `UsuarioId` se definen en `AppDbContext.OnModelCreating` y se materializan en `RentaFacil.API/Migrations/` (migración `InitialSqlServer`) — esa es la fuente de verdad del DDL. Para detalle campo por campo de cada entidad ver `glosario.md`.

```
Inquilinos ||--o{ Contratos : posee          (FK InquilinoId, borrado RESTRICT)
Inmuebles  ||--o{ Unidades  : contiene        (FK InmuebleId,  borrado CASCADE)
Unidades   ||--o{ Contratos : alquila         (FK UnidadId,    borrado RESTRICT)
Contratos  ||--o{ Pagos     : recibe          (FK ContratoId,  borrado CASCADE)
```

Reglas de borrado (definidas en `OnModelCreating`):
- Borrar un **Inmueble** elimina en cascada sus **Unidades**.
- Borrar un **Contrato** elimina en cascada sus **Pagos**.
- NO se puede borrar un **Inquilino** con Contratos ni una **Unidad** con Contratos (RESTRICT) — primero hay que quitar/cerrar los contratos.
- `Inmueble.MontoRenta` solo aplica a `Tipo == Unico`; en `Multiple` la renta vive en cada `Unidad`.
- `UsuarioId` está en las 5 entidades de `renta.*` y **sí** se usa para filtrar cada lectura/escritura en los Repositories (IDOR/BOLA cerrado, indexado) — ver `errores-conocidos.md`.

## Lo que NO existe (y no hay que crear sin que lo pidan)
- No hay caché de ningún tipo.
- No hay Docker en uso real (está en el plan para Fase 2, pero no hay `Dockerfile`/`docker-compose.yml` en el repo).
- No hay paginación en los `GetAll()` de la API.
- No hay CI/CD configurado (no hay workflows en `.github/workflows/`).
- No hay multiusuario real más allá de roles básicos (`Administrador`/`Propietario`) — el registro de cuentas vía `/api/auth/registrar` exige ya estar autenticado, no es self-service público.

(Autenticación JWT, filtrado por `UsuarioId`, y auditoría de cambios — que antes estaban en esta lista — ya están implementados; ver el resto de este documento y `decisiones.md`.)
