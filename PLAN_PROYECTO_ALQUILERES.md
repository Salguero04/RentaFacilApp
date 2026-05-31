# 🏠 RentaFácil — Plan de Proyecto
> App de Registro y Emisión de Recibos para Inquilinos  
> Cliente: Uso personal (Mario Salguero) → futuro: multiusuario  
> Versión del plan: 1.0 | Fecha: Mayo 2026

---

## 📌 Índice

1. [Visión General](#1-visión-general)
2. [Stack Tecnológico](#2-stack-tecnológico)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Estructura de Carpetas](#4-estructura-de-carpetas)
5. [Módulos y Funcionalidades](#5-módulos-y-funcionalidades)
6. [Modelos de Datos](#6-modelos-de-datos)
7. [Arquitectura por Capas (guía del compañero adaptada a C#)](#7-arquitectura-por-capas)
8. [Generación de Recibos con QuestPDF](#8-generación-de-recibos-con-questpdf)
9. [Docker y Microservicios](#9-docker-y-microservicios)
10. [Fases de Desarrollo](#10-fases-de-desarrollo)
11. [Wireframes Funcionales Identificados](#11-wireframes-funcionales-identificados)

---

## 1. Visión General

**RentaFácil** es una aplicación multiplataforma (celular + escritorio) para que arrendadores gestionen:

- Sus **propiedades** (casas, departamentos, edificios)
- Sus **inquilinos**
- Sus **contratos** de arrendamiento
- El **estado de pagos** mensual de cada inquilino
- La **emisión de recibos** en PDF (Ticket 80mm o Carta A4)
- Los **ingresos** por propiedad y mes

**Motivación:** Existe una app similar de pago (con 1 mes gratis). Este proyecto replica esas funcionalidades con tecnología propia, empezando como uso personal y escalando a multiusuario en fases.

---

## 2. Stack Tecnológico

| Capa | Tecnología | Razón |
|------|------------|-------|
| Frontend / UI | .NET MAUI Blazor Hybrid | Una sola base de código para Android, iOS, Windows y Mac |
| Backend API | ASP.NET Core Web API (.NET 8) | C# nativo, robusto, excelente con EF Core |
| Base de Datos (Fase 1) | SQLite (local en el dispositivo) | Sin necesidad de servidor, perfecto para prueba personal |
| Base de Datos (Fase 2) | SQL Server / PostgreSQL | Cuando se escale a multiusuario con Docker |
| ORM | Entity Framework Core | Migraciones, LINQ, Code-First |
| Generación de PDF | QuestPDF | Recibos en formato Ticket (80mm) y Carta (A4) |
| Autenticación | JWT + ASP.NET Identity | Login seguro, preparado para OAuth con Google |
| Contenedores | Docker + Docker Compose | Microservicios en Fase 2 |
| Comunicación | REST API (JSON) | Simple, estándar, fácil de testear |

---

## 3. Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────┐
│              MAUI Blazor Hybrid (UI)                     │
│    Android │ iOS │ Windows │ macOS                        │
│    Páginas Blazor (.razor) + HttpClient                  │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP/REST (JSON)
                     ▼
┌─────────────────────────────────────────────────────────┐
│            ASP.NET Core Web API                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐  │
│  │  Auth    │  │Inquilinos│  │Inmuebles │  │Recibos │  │
│  │Middleware│  │ Service  │  │ Service  │  │Service │  │
│  └──────────┘  └──────────┘  └──────────┘  └────────┘  │
│              ┌──────────────────────┐                    │
│              │   Repositories (EF)  │                    │
│              └──────────┬───────────┘                    │
└─────────────────────────┼───────────────────────────────┘
                          │
              ┌───────────┴──────────┐
              │   SQLite (Fase 1)    │
              │   SQL Server (Fase 2)│
              └──────────────────────┘
```

---

## 4. Estructura de Carpetas

```
RentaFacil/
│
├── RentaFacil.API/                  ← Backend ASP.NET Core
│   ├── Models/                      ← Entidades / DTOs
│   │   ├── Inquilino.cs
│   │   ├── Inmueble.cs
│   │   ├── Contrato.cs
│   │   ├── Pago.cs
│   │   └── DTOs/
│   │       ├── InquilinoDto.cs
│   │       └── ContratoDto.cs
│   │
│   ├── Data/                        ← DbContext + Migrations
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   │
│   ├── Repositories/                ← Acceso a base de datos
│   │   ├── Interfaces/
│   │   │   ├── IInquilinoRepository.cs
│   │   │   └── IContratoRepository.cs
│   │   ├── InquilinoRepository.cs
│   │   └── ContratoRepository.cs
│   │
│   ├── Services/                    ← Lógica de negocio
│   │   ├── Interfaces/
│   │   │   ├── IInquilinoService.cs
│   │   │   └── IContratoService.cs
│   │   ├── InquilinoService.cs
│   │   ├── ContratoService.cs
│   │   └── ReciboService.cs        ← QuestPDF aquí
│   │
│   ├── Controllers/                 ← Handlers HTTP (equivalente al Handler del prompt Go)
│   │   ├── AuthController.cs
│   │   ├── InquilinosController.cs
│   │   ├── InmueblesController.cs
│   │   ├── ContratosController.cs
│   │   ├── PagosController.cs
│   │   └── RecibosController.cs
│   │
│   ├── Middleware/                  ← JWT Validation, Error Handling
│   │   └── JwtMiddleware.cs
│   │
│   └── Program.cs                  ← Arranque + DI + Routes
│
├── RentaFacil.MAUI/                 ← Frontend Blazor Hybrid
│   ├── Pages/
│   │   ├── Login.razor
│   │   ├── Dashboard.razor
│   │   ├── Inquilinos/
│   │   │   ├── ListaInquilinos.razor
│   │   │   └── RegistrarInquilino.razor
│   │   ├── Inmuebles/
│   │   │   ├── MisInmuebles.razor
│   │   │   ├── InmuebleUnico.razor
│   │   │   └── InmuebleMultiple.razor
│   │   ├── Contratos/
│   │   │   ├── MisContratos.razor
│   │   │   └── RegistrarContrato.razor
│   │   ├── Estado/
│   │   │   └── EstadoPagos.razor
│   │   └── Ingresos/
│   │       └── Ingresos.razor
│   │
│   ├── Services/                   ← Llamadas a la API
│   │   ├── ApiClient.cs
│   │   └── AuthService.cs
│   │
│   └── MauiProgram.cs
│
├── RentaFacil.Shared/               ← DTOs compartidos entre API y MAUI
│   └── Models/
│
├── docker-compose.yml               ← Orquestación (Fase 2)
├── Dockerfile.api
└── README.md
```

---

## 5. Módulos y Funcionalidades

Basado en el análisis de las imágenes de la app de referencia:

### 5.1 Autenticación
- Login con Email + Contraseña
- Recuperar contraseña
- Registro de cuenta nueva
- Login con Google (OAuth — Fase 2)
- JWT con expiración configurable

### 5.2 Inquilinos
- Registrar inquilino: foto (galería/cámara), nombre completo, CI/NIT/DNI, teléfono
- Listar inquilinos
- Editar y eliminar inquilino
- Buscar inquilino

### 5.3 Inmuebles
- **Tipo Único**: Casa o departamento independiente
  - Nombre de la unidad
  - Descripción / Dirección
  - Monto de renta
- **Tipo Múltiple**: Edificio, Complejo, Galería
  - Nombre del grupo
  - Descripción / Dirección
  - Gestión de unidades internas (ej: Dept 1, Dept 2...)
  - Medidores por unidad
- Opciones por inmueble: Medidores | Ver Unidades | Editar | Eliminar
- Contador: "Alquilado X/Y"

### 5.4 Contratos
- Seleccionar inquilino + inmueble
- Frecuencia de pago (Mensual, Semanal, etc.)
- Duración en meses (con botones + / -)
- Monto y Garantía en USD
- Fecha inicio → Fecha fin calculada automáticamente
- Día de pago
- Observaciones internas
- Listar contratos activos

### 5.5 Estado de Pagos
- Vista por fecha (día actual por defecto)
- Selector de mes/año
- Card por inquilino con:
  - Nombre + unidad
  - Periodo del contrato (ej: MAY-JUN/26)
  - F/PAGO (fecha de pago programada)
  - SERV (servicios extras, ej: luz)
  - Total | A Cuenta | Saldo
  - Estado de factura (SIN FACTURA / FACTURADO)
- **Indicadores de color:**
  - 🔵 Azul: Fecha de pago próxima
  - 🟡 Amarillo: Fecha de pago es hoy
  - 🔴 Rojo: Fecha de pago ya pasó
  - 🟢 Verde (barra): Pago completado
  - 🟢 Verde (badge): Factura entregada
  - 🔴 Rojo (badge): Factura sin entregar
- Porcentaje del total pagado (barra de progreso)
- Icono "carita" cuando todos pagaron

### 5.6 Ingresos
- Vista mensual por inmueble
- Alquileres + Servicios
- Total en USD por periodo

### 5.7 Recibos (QuestPDF)
- **Formato Ticket** (80mm Roll — para impresora térmica)
- **Formato Carta** (A4/Letter)
- Compartir por WhatsApp directamente
- Contenido del recibo (basado en imagen):
  - Lugar y fecha de expedición
  - Número de recibo
  - Nombre de quien paga
  - Cantidad en números y letras
  - Concepto (renta, periodo)
  - Firma/nombre de quien recibe

---

## 6. Modelos de Datos

```csharp
// Inquilino.cs
public class Inquilino
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; }
    public string Identificacion { get; set; }   // CI / NIT / DNI
    public string Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int UsuarioId { get; set; }           // dueño del registro
    public ICollection<Contrato> Contratos { get; set; }
}

// Inmueble.cs
public class Inmueble
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Direccion { get; set; }
    public TipoInmueble Tipo { get; set; }       // Unico | Multiple
    public decimal MontoRenta { get; set; }      // solo para Único
    public int UsuarioId { get; set; }
    public ICollection<Unidad> Unidades { get; set; }
}

public enum TipoInmueble { Unico, Multiple }

// Unidad.cs (para Inmueble Múltiple)
public class Unidad
{
    public int Id { get; set; }
    public string Nombre { get; set; }           // "Dept 1", "Local 3"
    public decimal MontoRenta { get; set; }
    public bool Ocupada { get; set; }
    public int InmuebleId { get; set; }
    public Inmueble Inmueble { get; set; }
}

// Contrato.cs
public class Contrato
{
    public int Id { get; set; }
    public int InquilinoId { get; set; }
    public int UnidadId { get; set; }            // o InmuebleId si es Único
    public decimal Monto { get; set; }
    public decimal Garantia { get; set; }
    public FrecuenciaPago Frecuencia { get; set; }
    public int DuracionMeses { get; set; }
    public int DiaPago { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public ICollection<Pago> Pagos { get; set; }
    public Inquilino Inquilino { get; set; }
}

public enum FrecuenciaPago { Mensual, Quincenal, Semanal }

// Pago.cs
public class Pago
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public decimal TotalMonto { get; set; }
    public decimal ACuenta { get; set; }
    public decimal Servicios { get; set; }
    public DateTime FechaPago { get; set; }
    public string Periodo { get; set; }          // "MAY-JUN/26"
    public bool Facturado { get; set; }
    public bool Completado { get; set; }
    public Contrato Contrato { get; set; }
}
```

---

## 7. Arquitectura por Capas

> Inspirada directamente en el prompt de tu compañero (Go):  
> `Model → Repository → Service → Handler → Routes → Main`  
> **Adaptada a C# / ASP.NET Core:**  
> `Model → Repository → Service → Controller → Program.cs`

### Capa 1: Models
Define las entidades y DTOs. No contiene lógica.

```csharp
// DTOs/ContratoDto.cs
public record CrearContratoDto(
    int InquilinoId,
    int UnidadId,
    decimal Monto,
    decimal Garantia,
    int DuracionMeses,
    int DiaPago,
    DateTime FechaInicio,
    string? Observaciones
);
```

### Capa 2: Repository
Solo habla con la base de datos. Sin lógica de negocio.

```csharp
public interface IContratoRepository
{
    Task<Contrato?> GetByIdAsync(int id);
    Task<IEnumerable<Contrato>> GetByUsuarioAsync(int usuarioId);
    Task<Contrato> CreateAsync(Contrato contrato);
    Task UpdateAsync(Contrato contrato);
    Task DeleteAsync(int id);
}

public class ContratoRepository : IContratoRepository
{
    private readonly AppDbContext _ctx;
    public ContratoRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Contrato>> GetByUsuarioAsync(int usuarioId)
        => await _ctx.Contratos
            .Include(c => c.Inquilino)
            .Where(c => c.Unidad.Inmueble.UsuarioId == usuarioId)
            .ToListAsync();
    // ...
}
```

### Capa 3: Service
Lógica de negocio. Usa el Repository, no DbContext directo.

```csharp
public class ContratoService : IContratoService
{
    private readonly IContratoRepository _repo;

    public ContratoService(IContratoRepository repo) => _repo = repo;

    public async Task<Contrato> CrearContratoAsync(CrearContratoDto dto)
    {
        var fechaFin = dto.FechaInicio.AddMonths(dto.DuracionMeses);
        var contrato = new Contrato
        {
            InquilinoId = dto.InquilinoId,
            UnidadId = dto.UnidadId,
            Monto = dto.Monto,
            Garantia = dto.Garantia,
            FechaInicio = dto.FechaInicio,
            FechaFin = fechaFin,
            DiaPago = dto.DiaPago,
            DuracionMeses = dto.DuracionMeses,
            Activo = true
        };
        return await _repo.CreateAsync(contrato);
    }
}
```

### Capa 4: Controller (= Handler en Go)
Recibe HTTP requests, llama al Service, devuelve respuesta.

```csharp
[ApiController]
[Route("api/contratos")]
[Authorize]                          // ← Middleware JWT aplicado aquí
public class ContratosController : ControllerBase
{
    private readonly IContratoService _service;

    public ContratosController(IContratoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearContratoDto dto)
    {
        var contrato = await _service.CrearContratoAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = contrato.Id }, contrato);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contrato = await _service.GetByIdAsync(id);
        return contrato is null ? NotFound() : Ok(contrato);
    }
}
```

### Capa 5: Program.cs (= Routes + Main en Go)
Registra todo: servicios, middleware, rutas, JWT.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Registrar DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=rentafacil.db"));   // Fase 1

// Registrar Repositories y Services (DI)
builder.Services.AddScoped<IInquilinoRepository, InquilinoRepository>();
builder.Services.AddScoped<IInquilinoService, InquilinoService>();
builder.Services.AddScoped<IContratoRepository, ContratoRepository>();
builder.Services.AddScoped<IContratoService, ContratoService>();
builder.Services.AddScoped<IReciboService, ReciboService>();

// JWT Auth (equivalente a middleware del prompt Go)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 8. Generación de Recibos con QuestPDF

### Instalar
```bash
dotnet add package QuestPDF
```

### Formato Ticket (80mm)
```csharp
public class ReciboTicketDocument : IDocument
{
    private readonly Pago _pago;
    public ReciboTicketDocument(Pago pago) => _pago = pago;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(80, 200, Unit.Millimetre);   // Rollo 80mm
            page.Margin(5, Unit.Millimetre);

            page.Content().Column(col =>
            {
                col.Item().AlignCenter().Text("RECIBO DE ARRENDAMIENTO")
                   .Bold().FontSize(12);

                col.Item().Text($"N°: {_pago.Id:D4}");
                col.Item().Text($"Fecha: {_pago.FechaPago:dd/MM/yyyy}");
                col.Item().Text($"Inquilino: {_pago.Contrato.Inquilino.NombreCompleto}");
                col.Item().Text($"Periodo: {_pago.Periodo}");
                col.Item().Text($"Monto: $ {_pago.TotalMonto:F2}");
                col.Item().Text($"A Cuenta: $ {_pago.ACuenta:F2}");
                col.Item().Text($"Saldo: $ {(_pago.TotalMonto - _pago.ACuenta):F2}");

                col.Item().PaddingTop(10).AlignCenter()
                   .Text("_________________________");
                col.Item().AlignCenter().Text("Firma de quien recibe");
            });
        });
    }
}
```

### Formato Carta (A4)
Similar pero con `page.Size(PageSizes.A4)` y diseño más formal con logo, tabla de conceptos y espacio para firma completa.

### Generar y retornar PDF
```csharp
// ReciboService.cs
public byte[] GenerarTicket(Pago pago)
{
    var document = new ReciboTicketDocument(pago);
    return document.GeneratePdf();
}

// RecibosController.cs
[HttpGet("{pagoId}/ticket")]
public async Task<IActionResult> DescargarTicket(int pagoId)
{
    var pago = await _pagoService.GetByIdAsync(pagoId);
    var pdf = _reciboService.GenerarTicket(pago);
    return File(pdf, "application/pdf", $"recibo_{pagoId}.pdf");
}
```

---

## 9. Docker y Microservicios

> Para **Fase 2** cuando se escale a multiusuario o se quiera desplegar en servidor.

### docker-compose.yml
```yaml
version: '3.8'

services:

  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=sqlserver;Database=RentaFacil;User=sa;Password=Tu_Password_123;
      - Jwt__Key=tu_clave_secreta_muy_larga_aqui
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Tu_Password_123
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

### Dockerfile.api
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RentaFacil.API/RentaFacil.API.csproj", "RentaFacil.API/"]
RUN dotnet restore "RentaFacil.API/RentaFacil.API.csproj"
COPY . .
WORKDIR "/src/RentaFacil.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentaFacil.API.dll"]
```

### Correr en local
```bash
docker-compose up --build
```

---

## 10. Fases de Desarrollo

### ✅ Fase 1 — MVP Personal (1-2 meses)
**Base de datos local SQLite, sin Docker, sin auth compleja**

- [ ] Crear solución con proyectos: `RentaFacil.API`, `RentaFacil.MAUI`, `RentaFacil.Shared`
- [ ] Configurar EF Core + SQLite + migraciones
- [ ] CRUD Inquilinos
- [ ] CRUD Inmuebles (Único y Múltiple)
- [ ] CRUD Contratos (con cálculo automático de fecha fin)
- [ ] Módulo Estado de Pagos con indicadores de color
- [ ] Generación de recibo Ticket (QuestPDF 80mm)
- [ ] Generación de recibo Carta (QuestPDF A4)
- [ ] UI básica en Blazor Hybrid (funcional, sin diseño fancy)
- [ ] Deploy en Android (APK de prueba)

### 🔄 Fase 2 — Multiusuario + Servidor (2-3 meses)
**Autenticación real, SQL Server, Docker**

- [ ] ASP.NET Identity + JWT completo
- [ ] Login con Google (OAuth 2.0)
- [ ] Migrar de SQLite a SQL Server
- [ ] Dockerizar API + DB
- [ ] Deploy en servidor (VPS / Azure / Railway)
- [ ] Módulo Ingresos con gráficas
- [ ] Notificaciones de vencimiento de pago
- [ ] Compartir recibo por WhatsApp (deep link)
- [ ] Medidores de servicios (agua, luz) por unidad

### 🚀 Fase 3 — Producto Completo (futuro)
- [ ] Suscripciones (Gratis / Pro)
- [ ] App iOS
- [ ] Dashboard web (Blazor Server o WASM)
- [ ] Reportes mensuales exportables
- [ ] Múltiples usuarios por cuenta (propietario + empleados)
- [ ] Firma digital en contratos (PDF firmado)

---

## 11. Wireframes Funcionales Identificados

Basado en el análisis de las imágenes compartidas:

| Pantalla | Elementos clave |
|----------|----------------|
| **Login** | Email + Password, botón Iniciar Sesión, Google OAuth, Crear Cuenta |
| **Menú lateral** | Inquilinos, Inmuebles, Contratos, Ingresos, Suscripción, Opinión, Soporte |
| **Mis Inmuebles** | Lista, badge "Múltiple/Único", contador Alquilado X/Y, botón Nuevo Inmueble |
| **Tipo de Inmueble** | Modal con opción Único o Múltiple |
| **Inmueble Único** | Nombre, Dirección, Monto Renta (con bandera USD) |
| **Inmueble Múltiple** | Nombre del Grupo, Dirección (las unidades se agregan después) |
| **Opciones Inmueble** | Bottom sheet: Medidores, Ver Unidades, Editar, Eliminar (rojo) |
| **Registrar Inquilino** | Foto (galería/cámara), Nombre, CI/NIT/DNI, Teléfono |
| **Mis Contratos** | Card: nombre inquilino, unidad, monto USD, Desde/Hasta, Día de Pago |
| **Registrar Contrato** | Inquilino + Inmueble (requeridos), frecuencia, duración, monto, garantía, fechas, observaciones |
| **Estado de Pagos** | Fecha, total pendientes, cards con barra de progreso y colores |
| **Leyenda de colores** | Modal explicando colores: próximo/hoy/vencido/facturado |
| **Formato Recibo** | Modal: Ticket 80mm, Carta A4, compartir por WhatsApp |
| **Ingresos** | Por mes (selector), por inmueble, Alquileres + Servicios |
| **Selector de fecha** | Month picker con año ajustable |

---

## 📝 Notas Finales

- **Nombre del proyecto**: Por definir. Opciones sugeridas: `RentaFácil`, `ArriendoApp`, `MiArrendador`, `InquilinoControl`
- **Referencia de la app existente**: La app analizada en las imágenes sirve como benchmark de funcionalidades. El objetivo es replicar su flujo UX pero con tecnología propia.
- **El prompt de tu compañero** define exactamente la misma arquitectura por capas que usaremos, solo que él lo hace en Go y tú lo harás en C#. La lógica es la misma: separar responsabilidades en Model → Repository → Service → Controller → Program.
- **Empezar simple**: Fase 1 sin Docker, sin auth, solo SQLite local. Esto te permite avanzar rápido y tener algo funcionando en el celular pronto.

---

*Plan generado el 28 de Mayo de 2026 — Versión 1.0*
