# Convenciones de código

## Estilo
- Idioma: español en código, comentarios, nombres de identificadores y UI. Mantener esto en cualquier código nuevo.
- Formato: `Nullable` e `ImplicitUsings` habilitados en todos los `.csproj`. [PENDIENTE: no hay `.editorconfig` ni formatter (dotnet-format/StyleCop) configurado en el repo — no asumir reglas de formato no escritas]
- Naming:
  - Clases/Entidades/Servicios/Repositorios: PascalCase en español (`Inquilino`, `InmuebleService`, `IContratoRepository`)
  - Interfaces prefijadas con `I` (`IInquilinoService`, `IPagoRepository`)
  - DTOs: `record` con el patrón `Crear{Entidad}Dto` (para creación) y `{Entidad}Dto` (para lectura), siempre en `RentaFacil.Shared/Models/`
  - Páginas Blazor: PascalCase, un archivo `.razor` por pantalla en `Components/Pages/`, nombradas por la entidad o acción (`Inquilinos.razor`, `CrearInquilino.razor`, `DetallePagos.razor`) — no usar el patrón `<Acción><Controlador>.cshtml` de proyectos MVC hermanos (ver `ClaudeCampeonatoatp.md`)
- Imports: `using` agrupados por proyecto/namespace al inicio del archivo, sin alias ni imports relativos especiales observados.

## Patrones que SÍ usamos
- Capas: `Model → Repository → Service → Controller → Program.cs` en `RentaFacil.API`. Los Controllers no deben hablar con `AppDbContext` directo (ver excepción en `errores-conocidos.md`).
- Inyección de dependencias por constructor, registrada como `Scoped` en `Program.cs`.
- DTOs inmutables (`record`) para todo lo que cruza la frontera API↔cliente.
- Bottom sheet (no modal de pantalla completa) para acciones contextuales en listados móviles (`Inquilinos.razor`, `Inmuebles.razor`).
- Migraciones EF Core aplicadas automáticamente al iniciar la API (`context.Database.Migrate()` en `Program.cs`), no a mano.

## Patrones a evitar (detectados como excepción, no como regla)
- `UnidadesController` accede a `AppDbContext` directamente en vez de pasar por Repository/Service — es la única excepción al patrón de capas en el repo. No usarlo como plantilla para controllers nuevos.
- No hay `[Authorize]` en ningún Controller — no es una decisión consciente de "no usar auth", es la ausencia total de autenticación de servidor (ver `errores-conocidos.md` y la sección "Pendiente" de `CLAUDE.md`).

## Tests
- Dónde van: `RentaFacil.Tests/`, un archivo por entidad o agrupados (`OtherServiceTests.cs` cubre Inmueble/Contrato/Pago).
- Qué se testea: los Services, mockeando el Repository correspondiente con Moq y verificando con FluentAssertions (`result.Should().HaveCount(...)`, etc.). No hay tests de Controllers ni de integración contra una base de datos real.
- [PENDIENTE: no hay regla de cobertura mínima ni de "todo cambio debe traer test" documentada — confirmar con el usuario si se quiere adoptar.]

## Commits
- Histórico actual muy corto (2 commits: `first commit: Proyecto inicial RentaFacil`, `chore: setup local and production modes, generate APK V1.0.3`). El segundo sugiere un prefijo tipo `chore:` pero no hay suficiente evidencia para afirmar que el proyecto sigue Conventional Commits de forma estricta. [PENDIENTE: confirmar si se quiere adoptar Conventional Commits formalmente.]
- Regla de respaldo: cada vez que se genera un nuevo `.apk` y se sube de versión, se debe hacer `git commit` (y opcionalmente `push`) para dejar respaldado el código exacto que generó ese APK (ver decisión de versionado en `decisiones.md`).
