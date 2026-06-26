using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentaFacil.API.Data;
using RentaFacil.API.Services.Interfaces;
using QuestPDF.Infrastructure;

// Configure QuestPDF License (Community)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure SQLite DbContext
builder.Services.AddScoped<RentaFacil.API.Data.AuditoriaInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite("Data Source=rentafacil.db")
           .AddInterceptors(sp.GetRequiredService<RentaFacil.API.Data.AuditoriaInterceptor>()));

// Dependency Injection
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IInquilinoRepository, RentaFacil.API.Repositories.InquilinoRepository>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IInquilinoService, RentaFacil.API.Services.InquilinoService>();
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IInmuebleRepository, RentaFacil.API.Repositories.InmuebleRepository>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IInmuebleService, RentaFacil.API.Services.InmuebleService>();
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IContratoRepository, RentaFacil.API.Repositories.ContratoRepository>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IContratoService, RentaFacil.API.Services.ContratoService>();
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IPagoRepository, RentaFacil.API.Repositories.PagoRepository>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IPagoService, RentaFacil.API.Services.PagoService>();
builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IReciboService, RentaFacil.API.Services.ReciboService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RentaFacil.API.Repositories.Interfaces.IUsuarioRepository, RentaFacil.API.Repositories.UsuarioRepository>();
builder.Services.AddScoped<IAutenticacionService, RentaFacil.API.Services.AutenticacionService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key. Ejecuta: dotnet user-secrets set \"Jwt:Key\" \"<clave>\" --project RentaFacil.API");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, QueueLimit = 0 }));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Comentado para permitir conexiones HTTP desde el celular en LAN

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self' https://cdn.jsdelivr.net; font-src 'self' https://cdn.jsdelivr.net";
    await next();
});

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    if (!context.Usuarios.Any())
    {
        var seedUsuario = app.Configuration["SeedAdmin:Usuario"];
        var seedPassword = app.Configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(seedUsuario) || string.IsNullOrWhiteSpace(seedPassword))
        {
            throw new InvalidOperationException(
                "Falta configurar SeedAdmin:Usuario / SeedAdmin:Password. Ejecuta:\n" +
                "  dotnet user-secrets set \"SeedAdmin:Usuario\" \"<usuario>\" --project RentaFacil.API\n" +
                "  dotnet user-secrets set \"SeedAdmin:Password\" \"<password>\" --project RentaFacil.API");
        }

        var admin = new RentaFacil.API.Models.Usuario
        {
            NombreUsuario = seedUsuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            Rol = RentaFacil.Shared.AppRoles.Administrador,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        context.Usuarios.Add(admin);
        context.SaveChanges();

        // Remapea datos preexistentes (UsuarioId = 1 hardcodeado) al usuario sembrado real.
        // No-op en una base de datos nueva, ya que estas tablas estarían vacías.
        foreach (var inquilino in context.Inquilinos) inquilino.UsuarioId = admin.Id;
        foreach (var inmueble in context.Inmuebles) inmueble.UsuarioId = admin.Id;
        context.SaveChanges();

        if (!context.Inquilinos.Any())
        {
            var inq = new RentaFacil.API.Models.Inquilino { NombreCompleto = "Mario Salguero (Dummy)", Identificacion = "1234567", Telefono = "555-0100", FechaRegistro = DateTime.Now, UsuarioId = admin.Id };
            context.Inquilinos.Add(inq);
            context.SaveChanges();

            var inm = new RentaFacil.API.Models.Inmueble { Nombre = "Edificio Central", Direccion = "Av. Principal 123", Tipo = RentaFacil.Shared.Enums.TipoInmueble.Multiple, MontoRenta = 0, UsuarioId = admin.Id };
            context.Inmuebles.Add(inm);
            context.SaveChanges();

            var uni = new RentaFacil.API.Models.Unidad { Nombre = "Apt 1A", MontoRenta = 500, Ocupada = true, InmuebleId = inm.Id };
            context.Unidades.Add(uni);
            context.SaveChanges();

            var con = new RentaFacil.API.Models.Contrato { InquilinoId = inq.Id, UnidadId = uni.Id, Monto = 500, Garantia = 500, Frecuencia = RentaFacil.Shared.Enums.FrecuenciaPago.Mensual, DuracionMeses = 12, DiaPago = 5, FechaInicio = DateTime.Now, FechaFin = DateTime.Now.AddMonths(12), Activo = true };
            context.Contratos.Add(con);
            context.SaveChanges();

            var pag = new RentaFacil.API.Models.Pago { ContratoId = con.Id, TotalMonto = 500, ACuenta = 200, Servicios = 0, FechaPago = DateTime.Now, Periodo = "MAY-26", Facturado = false, Completado = false };
            context.Pagos.Add(pag);
            context.SaveChanges();
        }
    }
}

app.Run();
