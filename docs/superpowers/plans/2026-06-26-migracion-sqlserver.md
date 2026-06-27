# Plan de implementación: Migración SQL Server + Schemas — RentaFácil

**Fecha:** 2026-06-26
**Estado:** en ejecución (después del merge de `feature/seguridad-auditoria`)
**Rama:** `feature/migracion-sqlserver` (creada desde `main`)
**Archivo destino en repo:** `docs/superpowers/plans/2026-06-26-migracion-sqlserver.md`

---

## Objetivo

Migrar de SQLite a SQL Server como base de datos principal, introducir 4 schemas organizacionales fijos (`auth`, `renta`, `config`, `audit`), y sentar la infraestructura para que en el futuro el salto a BD-por-tenant sea un cambio localizado en el factory — sin tocar repositories, services ni controllers.

**Decisión confirmada (2026-06-26):** SQL Server reemplaza a MySQL en todos los entornos (local + producción). MySQL queda descartado.

**Lo que NO cambia:**
- La estrategia de multitenancy por fila (`UsuarioId` en cada entidad de `renta.*`)
- Los repositories, services y controllers
- El filtro IDOR ya implementado
- La lógica de negocio

---

## Strings de conexión por entorno

```
Trabajo:  Server=GGCBOADMWRK025\SQLEXPRESS;Database=RentaFacil;...
Casa:     Server=DESKTOP-07M16LE\LOCALDB#9246A1FB;Database=RentaFacil;...
Prod:     (definir cuando se elija hosting)
```

Ambas usan `Integrated Security=true` para desarrollo local — sin credenciales hardcodeadas.

---

## Schemas organizacionales (fijos, no por tenant)

| Schema | Responsabilidad | Tiene `UsuarioId` |
|--------|----------------|-------------------|
| `auth` | Identidad, acceso, tokens | No — estas tablas SON los usuarios |
| `renta` | Dominio principal del negocio | Sí — filtro de tenant por fila |
| `config` | Catálogos y datos globales compartidos | No — datos de solo lectura globales |
| `audit` | Trazabilidad de cambios y accesos | No — lo llena el interceptor |

Estos schemas son **estáticos y fijos** — no crecen con los usuarios. Son organización, no multitenancy.

---

## Orden de dependencias

```
Task 1 → Paquete SQL Server + connection strings por entorno
Task 2 → Schemas en OnModelCreating (auth / renta / config / audit)
Task 3 → decimal(18,2) en todas las entidades monetarias
Task 4 → Migración inicial SQL Server (reemplaza las de SQLite)
Task 5 → appsettings por entorno (Trabajo / Casa / Producción)
Task 6 → IDbContextFactory registrado (base para BD-por-tenant futuro)
Task 7 → Seed admin en SQL Server + verificación end-to-end
Task 8 → Actualizar tests
Task 9 → Verificación final + actualizar CLAUDE.md
```

---

## Task 1 — Paquete SQL Server + quitar SQLite

**Archivos:** `RentaFacil.API/RentaFacil.API.csproj`, `RentaFacil.API/Program.cs`

```bash
# Desde RentaFacil.API/
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
```

En `Program.cs`, cambiar el provider a `UseSqlServer` con la tabla de migraciones en el schema `config`:

```csharp
opt.UseSqlServer(
    builder.Configuration.GetConnectionString("Default"),
    sqlOpt => sqlOpt.MigrationsHistoryTable("__EFMigrationsHistory", "config"));
```

**Verificación:** `dotnet build RentaFacil.API` sin errores.

---

## Task 2 — Schemas en `OnModelCreating`

**Archivo:** `RentaFacil.API/Data/AppDbContext.cs`

Agregar `ToTable("Nombre", "schema")` a cada entidad: `Usuario` → `auth`; `Inquilino`/`Inmueble`/`Unidad`/`Contrato`/`Pago` → `renta`. Confirmar/crear índices en `UsuarioId` de cada entidad de `renta.*` (SQL Server no indexa FKs automáticamente, y el `WHERE UsuarioId = X` corre en cada request).

**Verificación:** `dotnet build RentaFacil.API`.

---

## Task 3 — `decimal(18,2)` en entidades monetarias

**Archivos:** `RentaFacil.API/Models/Contrato.cs`, `RentaFacil.API/Models/Pago.cs` (y `Inmueble.MontoRenta`, `Unidad.MontoRenta`).

Agregar `[Column(TypeName = "decimal(18,2)")]` a todos los campos de dinero. SQLite ignoraba esta anotación; SQL Server la respeta.

**Verificación:** `dotnet build RentaFacil.API`.

---

## Task 4 — Migración inicial SQL Server

```bash
# Desde RentaFacil.API/
rm -rf Migrations/          # las migraciones SQLite ya no aplican (quedan en git history)
dotnet ef migrations add InitialSqlServer
```

Revisar que la migración generada incluya `CREATE SCHEMA [auth]`/`[renta]`, las tablas en su schema, `decimal(18,2)` y los índices de `UsuarioId`. Aplicar con `dotnet ef database update` (o dejar que el `Migrate()` automático del arranque lo haga).

**Verificación:** schemas `auth`/`renta` y sus tablas existen en la BD.

---

## Task 5 — `appsettings` por entorno

- `appsettings.json` → sin connection string real (placeholder).
- Connection string real por máquina vía **User Secrets** (`ConnectionStrings:Default`).
- Producción → variable de entorno `ConnectionStrings__Default`, nunca en el repo.

**Verificación:** `dotnet run --project RentaFacil.API` conecta sin errores.

---

## Task 6 — `IDbContextFactory` registrado

**Archivo:** `RentaFacil.API/Program.cs`

Registrar `AddDbContextFactory<AppDbContext>` con el mismo `UseSqlServer`. No cambia el comportamiento actual, pero es el punto de extensión para el futuro `TenantDbContextFactory` (BD-por-tenant) sin tocar repositories/services.

**Verificación:** `dotnet build RentaFacil.API`.

---

## Task 7 — Seed admin en SQL Server + verificación end-to-end

El seed existente (de la Task 9 de seguridad) debería funcionar sin cambios (es EF Core puro). Arrancar la API con BD vacía → aplica migraciones, crea schemas, siembra admin desde User Secrets y datos dummy en `renta.*`. Smoke test: login → JWT → `GET /api/inquilinos` → datos visibles bajo los schemas correctos.

---

## Task 8 — Actualizar tests

Los tests usan `SqliteConnection(":memory:")` (Task 11 de seguridad). SQL Server no tiene in-memory. **Opción A (recomendada ahora):** `UseInMemoryDatabase` de EF Core para tests de lógica de negocio. **Opción B (futuro):** `Testcontainers.MsSql` cuando se necesite validar comportamiento específico de SQL Server.

**Verificación:** `dotnet test RentaFacil.Tests` en verde.

---

## Task 9 — Verificación final + actualizar `CLAUDE.md`

Checklist: build API + MAUI Android, tests verdes, schemas/tablas/seed verificados en la BD, smoke test end-to-end. Actualizar `CLAUDE.md` y `docs/contexto/` (decisiones, arquitectura, glosario, errores-conocidos) de SQLite/MySQL → SQL Server.

**Commit final:** `feat: migración SQL Server — schemas auth/renta/config/audit, decimal(18,2), IDbContextFactory`

---

## Fuera de alcance (explícitamente)

- Docker para SQL Server (ya instalado en ambas máquinas).
- Schemas `config.*`/`audit.*` con entidades reales — se agregan cuando existan.
- BD-por-tenant — la infraestructura (`IDbContextFactory`) queda lista; la lógica del `TenantDbContextFactory` se implementa cuando haya más de un propietario real.
- Globalización (MoneyFormatter, InvariantCulture, .resx) — spec separado.
- UX/UI — spec separado.

---

## Nota sobre el camino a BD-por-tenant

Cuando se quiera saltar a BD-por-tenant: crear un `TenantDbContextFactory` que lea el `UsuarioId` del JWT y elija la connection string; modificar el registro en `Program.cs` para usar el factory; crear una BD por cada propietario (con los mismos 4 schemas). Repositories/services/controllers **no cambian**. Los 4 schemas se replican idénticos en cada BD — solo cambia en qué BD viven los datos.
