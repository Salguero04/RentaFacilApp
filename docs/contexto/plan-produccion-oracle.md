# Producción en Oracle Cloud + Deploy simple + Correos (Brevo) + Versionado de App — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Llevar la API de RentaFácil a producción en la VM Oracle Cloud (Ubuntu ARM64) con Docker + HTTPS, un flujo de deploy de un solo comando (`update.sh`), recuperación de contraseñas por correo (Brevo) y bloqueo de versiones obsoletas de la app MAUI.

**Architecture:** La API .NET 10 corre en contenedor nativo ARM64; SQL Server corre en contenedor **emulado x86_64** (`platform: linux/amd64`) en la misma VM vía docker-compose. Cloudflare hace de proxy HTTPS delante del origen HTTP. El deploy es `git pull` + `docker compose up -d --build` empaquetado en `update.sh`. Los correos salen por SMTP de Brevo detrás de `IEmailService`. La app MAUI consulta `GET /api/config/version` al arrancar y se bloquea si su versión es menor a la mínima.

**Tech Stack:** .NET 10, EF Core 10, SQL Server 2022 (linux/amd64 emulado), Docker + Docker Compose, Cloudflare (proxy/DNS), Brevo SMTP (`System.Net.Mail.SmtpClient`), JWT (infra existente), xUnit+Moq+FluentAssertions.

## Diagnóstico previo (2026-07-09) — qué existe y qué falta

| Requisito de la instrucción | Estado en el repo |
|---|---|
| Dockerfile / docker-compose.yml | ❌ **No existen** (confirmado con glob; `arquitectura.md` "Lo que NO existe" coincide) |
| `app.UseHttpsRedirection()` | ⚠️ Comentado a propósito (`Program.cs:133`, decisión LAN Fase 1) — falta descomentar **+ `UseForwardedHeaders`** (sin esto, detrás de Cloudflare hay bucle de redirección) |
| CORS restringido | ❌ `AllowAnyOrigin/AnyMethod/AnyHeader` (`Program.cs:114-122`) — falta hacerlo configurable |
| Deploy keys / clonación / update.sh | ❌ Nada en el servidor (runbook nuevo) |
| Migraciones EF al arrancar | ✅ Ya existe (`context.Database.Migrate()` en `Program.cs:163`) — funciona igual en contenedor |
| `IEmailService` / Brevo / recuperar-password | ❌ No existe nada de correos (grep: 0 matches de Smtp/Email/Brevo) |
| Google OAuth: migración `GoogleId` + PasswordHash nullable + `login-google` con `Google.Apis.Auth` | ✅ **YA IMPLEMENTADO Y MERGEADO** (2026-07-07, `main` `b2851c8`: migración `AgregarGoogleIdUsuario`, `IValidadorTokenGoogle`, endpoint con 503/403/401, 84/84 tests). Solo queda verificación — Tarea 12 |
| `ConfigController` + `GET /api/config/version` | ❌ No existe |
| Detección de versión en MAUI + bloqueo | ❌ No existe (`ApplicationDisplayVersion` sí está en `RentaFacil.MAUI.csproj`) |
| URL de producción en el cliente | ⚠️ `ApiConfig.cs:14` tiene IP hardcodeada `http://200.126.17.232:5295` — hay que apuntarla al dominio HTTPS |

## Integración con el plan del módulo inquilino (`docs/contexto/plan-modulo-inquilino.md`)

Los dos planes son **independientes y pueden ejecutarse en cualquier orden**, pero se tocan en estos puntos — quien ejecute este plan debe conocerlos:

1. **Migraciones:** si el módulo inquilino se implementa después del deploy, `./update.sh` (Tarea 7) aplica su migración `ModuloInquilino` automáticamente al reiniciar el contenedor (la API migra al arrancar). Nada extra en el servidor.
2. **Recuperación de contraseña (Tarea 9) y cuentas de inquilinos:** la recuperación funciona para cualquier `Usuario` con `Email`. El registro self-service del inquilino captura un **email opcional** justo por esto (Tarea 4 del plan del módulo) — un inquilino sin email no puede recuperar contraseña por correo (tendrá que pedirla al arrendador/administrador).
3. **Roles y endpoints anónimos:** la Tarea 1 del plan inquilino restringe TODOS los controllers de arrendador a `Administrador/Propietario`. El `ConfigController` de este plan (Tarea 13) es `[AllowAnonymous]` a propósito y NO debe recibir esa restricción; los `api/mi/*` del módulo exigen rol `Inquilino`. No hay conflicto, pero el implementador de un plan no debe "corregir" los atributos del otro.
4. **Versionado de la app (Tareas 13-16):** cuando el módulo inquilino se publique en un APK nuevo, subir `ApplicationDisplayVersion` y evaluar subir `VersionApp:MinVersionAndroid` en el `.env` del servidor para forzar la actualización (los valores iniciales de este plan son los de la instrucción original).
5. **CORS y clientes:** los inquilinos usan los mismos clientes MAUI/Web — no agregan orígenes nuevos.
6. **SignalR:** el plan del módulo reemplaza `Clients.All` por grupos por usuario (su Tarea 7b). Sin impacto en las tareas de este plan.

**Dos riesgos/correcciones detectados en la propia instrucción (decisiones para el usuario, ver Tarea 4 y Tarea 5):**
1. **SQL Server emulado en ARM64 (Ampere A1) es frágil**: la imagen oficial `mcr.microsoft.com/mssql/server` bajo qemu funciona en muchos casos pero es lenta y a veces inestable (Azure SQL Edge, la alternativa ARM nativa, fue retirada en 2025). El plan incluye un **smoke test GATE (Tarea 4)** antes de continuar; si falla, el fallback documentado es usar la VM x86 "always free" (VM.Standard.E2.1.Micro) solo para SQL Server.
2. **DuckDNS NO se puede poner detrás de Cloudflare**: Cloudflare exige un dominio propio cuyos nameservers controles; no puedes agregar `tuapp.duckdns.org` a una cuenta de Cloudflare. Opciones en la Tarea 5: (A recomendada) comprar un dominio (~$10/año) y usar Cloudflare free, o (B) quedarse con DuckDNS **sin** Cloudflare usando Caddy + Let's Encrypt en el servidor.

## Global Constraints

- Código, comentarios, mensajes de UI y de commit **en español**.
- Capas estrictas `Model → Repository → Service → Controller`; nada de `AppDbContext` en controllers.
- DTOs `record` en `RentaFacil.Shared/Models/`.
- `FallbackPolicy = RequireAuthenticatedUser` global: todo endpoint público nuevo necesita `[AllowAnonymous]` explícito (y rate limit `"auth"` si es de autenticación).
- Ningún secreto commiteado: local → user-secrets; servidor → archivo `.env` (git-ignored) leído por docker-compose.
- NO buildear `RentaFacil.slnx` completo (NETSDK1047 multi-RID MAUI). Verificar con builds por proyecto + `dotnet test RentaFacil.Tests` (hoy **84/84** verdes; no debe bajar).
- SQL Server con schemas `auth`/`renta`/`config`/`audit` se mantiene (decisión vigente en `decisiones.md`) — por eso la emulación amd64, no un cambio de motor.
- Versionado de la instrucción, copiar EXACTO en config: `minVersionAndroid: "1.0.1"`, `latestVersionAndroid: "1.0.3"`, `forceUpdate: true`, `updateUrl` apuntando al `.apk`.
- Endpoints nuevos de esta instrucción: `POST /api/auth/recuperar-password`, `POST /api/auth/restablecer-password`, `GET /api/config/version`.
- Puertos: la API escucha 8080 dentro del contenedor, mapeado al **80 del host** (Cloudflare solo proxya puertos estándar: 80/443/8080…, NO 5295).

---

## FASE 1 — Infraestructura, Docker y seguridad perimetral

### Task 1: Dockerfile de la API + docker-compose + .env de ejemplo (en el repo)

**Files:**
- Create: `RentaFacil.API/Dockerfile`
- Create: `docker-compose.yml` (raíz del repo)
- Create: `.env.example` (raíz del repo)
- Modify: `.gitignore` (agregar `.env`)
- Modify: `.dockerignore` (crear, raíz)

**Interfaces:**
- Produces: servicio compose `api` (puerto host 80 → contenedor 8080) y `sqlserver` (interno, `platform: linux/amd64`), red interna donde la API resuelve el host `sqlserver`.

- [ ] **Step 1: Crear `RentaFacil.API/Dockerfile`**

```dockerfile
# Build (corre nativo en ARM64; el SDK .NET 10 soporta linux-arm64)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo lo que la API necesita (API + Shared) para cachear el restore
COPY RentaFacil.Shared/RentaFacil.Shared.csproj RentaFacil.Shared/
COPY RentaFacil.API/RentaFacil.API.csproj RentaFacil.API/
RUN dotnet restore RentaFacil.API/RentaFacil.API.csproj

COPY RentaFacil.Shared/ RentaFacil.Shared/
COPY RentaFacil.API/ RentaFacil.API/
RUN dotnet publish RentaFacil.API/RentaFacil.API.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RentaFacil.API.dll"]
```

- [ ] **Step 2: Crear `.dockerignore`** (raíz)

```
**/bin
**/obj
**/betas APKs
**/.git
**/.superpowers
**/docs
**/*.md
.env
```

- [ ] **Step 3: Crear `docker-compose.yml`** (raíz)

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    # La VM es ARM64 (Ampere A1); SQL Server solo publica imagen x86_64.
    # Este flag fuerza emulación qemu — ver Tarea 4 (smoke test GATE).
    platform: linux/amd64
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SA_PASSWORD}"
      MSSQL_PID: "Express"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q 'SELECT 1' || exit 1"]
      interval: 15s
      timeout: 10s
      retries: 10
      start_period: 90s
    restart: unless-stopped

  api:
    build:
      context: .
      dockerfile: RentaFacil.API/Dockerfile
    ports:
      - "80:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      ConnectionStrings__Default: "Server=sqlserver;Database=RentaFacil;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=true;"
      Jwt__Key: "${JWT_KEY}"
      SeedAdmin__Usuario: "${SEED_ADMIN_USUARIO}"
      SeedAdmin__Password: "${SEED_ADMIN_PASSWORD}"
      Google__ClientId: "${GOOGLE_CLIENT_ID}"
      Google__PermitirRegistro: "${GOOGLE_PERMITIR_REGISTRO:-false}"
      Email__Usuario: "${BREVO_USUARIO}"
      Email__Password: "${BREVO_PASSWORD}"
      Email__Remitente: "${EMAIL_REMITENTE}"
      Email__UrlBaseRecuperacion: "${EMAIL_URL_BASE_RECUPERACION}"
      Cors__AllowedOrigins__0: "${CORS_ORIGIN_0}"
      Cors__AllowedOrigins__1: "${CORS_ORIGIN_1:-}"
    depends_on:
      sqlserver:
        condition: service_healthy
    restart: unless-stopped

volumes:
  sqldata:
```

- [ ] **Step 4: Crear `.env.example`** (raíz; documenta cada variable, SIN valores reales)

```bash
# Copiar a .env en el servidor y completar. .env está git-ignored.
SA_PASSWORD=CambiaEsta-Passw0rd-Fuerte
JWT_KEY=una-clave-de-al-menos-32-caracteres-aleatorios
SEED_ADMIN_USUARIO=dueno
SEED_ADMIN_PASSWORD=CambiaEstaClave123!
GOOGLE_CLIENT_ID=
GOOGLE_PERMITIR_REGISTRO=false
BREVO_USUARIO=
BREVO_PASSWORD=
EMAIL_REMITENTE=no-reply@tudominio.com
EMAIL_URL_BASE_RECUPERACION=https://tudominio.com
CORS_ORIGIN_0=https://tudominio.com
CORS_ORIGIN_1=
```

- [ ] **Step 5: Agregar `.env` a `.gitignore`** (una línea al final: `.env`)

- [ ] **Step 6: Verificar build local de la imagen de la API** (en Windows con Docker Desktop si está disponible; si no, se verifica en el servidor en la Tarea 3)

Run: `docker build -f RentaFacil.API/Dockerfile -t rentafacil-api .`
Expected: `Successfully built` / `naming to docker.io/library/rentafacil-api` sin errores.

- [ ] **Step 7: Commit**

```bash
git add RentaFacil.API/Dockerfile docker-compose.yml .env.example .dockerignore .gitignore
git commit -m "feat: Dockerfile de la API y docker-compose con SQL Server emulado amd64"
```

### Task 2: Endurecer `Program.cs` — ForwardedHeaders, HTTPS redirect y CORS configurable

**Files:**
- Modify: `RentaFacil.API/Program.cs` (CORS líneas ~113-122, pipeline líneas ~133-139)
- Modify: `RentaFacil.API/appsettings.json` (sección `Cors`)
- Modify: `docs/contexto/errores-conocidos.md` y `docs/contexto/decisiones.md` (la entrada "CORS abierto y HTTPS off" pasa a "ya RESUELTO" con fecha — regla de cierre de CLAUDE.md)

**Interfaces:**
- Consumes: variables `Cors__AllowedOrigins__N` del compose (Task 1).
- Produces: API que detrás del proxy de Cloudflare reporta `Request.IsHttps == true` y `RemoteIpAddress` real (el rate limiter por IP ya existente vuelve a ser efectivo).

- [ ] **Step 1: agregar sección a `appsettings.json`**

```json
"Cors": {
  "AllowedOrigins": []
}
```
(Vacío = comportamiento dev actual `AllowAnyOrigin`; con orígenes = solo esos. Así el dev local no se rompe.)

- [ ] **Step 2: reemplazar el bloque CORS en `Program.cs`**

```csharp
// CORS: en producción se restringe a los orígenes configurados (Cors:AllowedOrigins);
// sin configuración (dev local / clientes nativos MAUI que no envían Origin) queda abierto.
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (origenesPermitidos.Length > 0)
            policy.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
```

- [ ] **Step 3: ForwardedHeaders + HTTPS redirect en el pipeline** — ANTES de `app.UseCors(...)`:

```csharp
// Detrás del proxy (Cloudflare/Caddy) el esquema real y la IP del cliente llegan en
// X-Forwarded-Proto / X-Forwarded-For. Sin esto, UseHttpsRedirection entra en bucle
// y el rate limiter ve la IP del proxy en vez de la del cliente.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Proxies desconocidos: en Docker el proxy no es localhost, hay que limpiar las listas.
    KnownNetworks = { },
    KnownProxies = { }
});

app.UseHttpsRedirection(); // Reactivado (antes comentado para LAN — Fase 1 local terminó)
```
`using Microsoft.AspNetCore.HttpOverrides;` arriba. **Eliminar** la línea comentada vieja `// app.UseHttpsRedirection();` y su comentario.

- [ ] **Step 4: build + tests**

Run: `dotnet build RentaFacil.API/RentaFacil.API.csproj && dotnet test RentaFacil.Tests`
Expected: 0 errores, 84/84.

- [ ] **Step 5: actualizar docs** — `errores-conocidos.md`: marcar la entrada "CORS abierto... y UseHttpsRedirection comentado" como **ya RESUELTO (fecha, este plan)**; `decisiones.md`: actualizar el **Estado** de la decisión "CORS abierto y HTTPS redirection deshabilitado".

- [ ] **Step 6: Commit** — `git commit -m "feat: ForwardedHeaders + HTTPS redirect + CORS configurable para producción"`

### Task 3: Runbook — preparar el servidor Oracle (SSH, Docker, firewall)

> Tareas de servidor: se ejecutan por SSH en la VM (usuario `ubuntu`). Cada paso muestra el comando exacto y qué esperar. **Pedir confirmación al usuario antes de este task** (toca infraestructura real).

- [ ] **Step 1: conectar y verificar arquitectura**

```bash
ssh ubuntu@<IP-PUBLICA-ORACLE>
uname -m        # esperado: aarch64
lsb_release -a  # esperado: Ubuntu 22.04/24.04
```

- [ ] **Step 2: instalar Docker + plugin compose (repo oficial de Docker)**

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker ubuntu && newgrp docker
docker --version && docker compose version   # esperado: versiones sin error
```

- [ ] **Step 3: habilitar emulación x86_64 (qemu binfmt) para el contenedor de SQL Server**

```bash
docker run --privileged --rm tonistiigi/binfmt --install amd64
docker run --rm --platform linux/amd64 alpine uname -m   # esperado: x86_64
```

- [ ] **Step 4: abrir puertos 80/443** — en DOS lugares (ambos obligatorios en Oracle):
  1. Consola web de Oracle Cloud → VCN → Security List de la subnet → Ingress Rules: TCP 80 y 443 desde `0.0.0.0/0`.
  2. En la VM (iptables de Ubuntu de Oracle van por delante de ufw):
```bash
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

### Task 4: Runbook GATE — smoke test de SQL Server emulado (decisión: seguir o fallback)

> **Este task decide si el resto del plan continúa tal cual.** SQL Server bajo qemu en Ampere puede fallar o ser inaceptablemente lento.

- [ ] **Step 1: levantar SOLO SQL Server y probarlo**

```bash
docker run -d --name mssql-test --platform linux/amd64 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Prueba-Fuerte-123!' -e MSSQL_PID=Express \
  mcr.microsoft.com/mssql/server:2022-latest
sleep 90 && docker ps --filter name=mssql-test   # esperado: STATUS Up (no Restarting)
docker exec mssql-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Prueba-Fuerte-123!' -C \
  -Q "CREATE DATABASE Smoke; USE Smoke; CREATE SCHEMA renta; SELECT name FROM sys.schemas WHERE name='renta';"
# esperado: fila "renta". Medir que el sqlcmd responda en segundos, no minutos.
docker rm -f mssql-test
```

- [ ] **Step 2: decidir**
  - ✅ Pasa y el rendimiento es tolerable → continuar con Task 5.
  - ❌ Falla (contenedor en crash-loop, sqlcmd cuelga) o es inusable → **STOP: presentar al usuario el fallback**: crear la VM x86 always-free (`VM.Standard.E2.1.Micro`) solo para SQL Server (mismo compose sin `platform`, la API se queda en la ARM apuntando a la IP privada de esa VM), o reabrir la decisión de motor de BD. No improvisar sin el usuario.

### Task 5: Runbook — DNS + HTTPS con Cloudflare (requiere decisión del usuario)

> **Decisión previa (preguntar al usuario):** Cloudflare exige dominio propio — DuckDNS no puede proxearse por Cloudflare.
> **Opción A (recomendada):** comprar dominio (~$10/año) → estos pasos.
> **Opción B:** DuckDNS sin Cloudflare → sustituir este task por Caddy en el compose (Caddy obtiene Let's Encrypt solo; el puerto 443 ya quedó abierto en Task 3) y saltar los pasos de Cloudflare.

- [ ] **Step 1 (A): agregar el dominio a Cloudflare** — crear cuenta free en cloudflare.com → "Add a site" → introducir el dominio → plan Free → Cloudflare muestra 2 nameservers → cambiarlos en el registrador del dominio → esperar propagación (minutos a horas).
- [ ] **Step 2 (A): registro DNS** — en Cloudflare → DNS → Add record: tipo `A`, nombre `api` (o `@`), contenido `<IP-PUBLICA-ORACLE>`, **Proxy status: Proxied (nube naranja)** — esto oculta la IP real y da HTTPS + anti-DDoS.
- [ ] **Step 3 (A): modo SSL** — Cloudflare → SSL/TLS → Overview → **"Flexible"** para empezar (Cloudflare→origen va por HTTP:80, que es lo que expone el compose). Anotar como endurecimiento posterior: subir a **"Full (strict)"** generando un certificado Origin CA de Cloudflare y montándolo en Kestrel.
- [ ] **Step 4: probar** — `curl -I https://api.tudominio.com/swagger` desde fuera → esperado `HTTP/2 200` (o 404 si swagger está solo en dev: probar `curl -s -o /dev/null -w "%{http_code}" https://api.tudominio.com/api/auth/login -X POST -H "Content-Type: application/json" -d "{}"` → esperado `400`/`401`, NO timeout).
- [ ] **Step 5: rellenar `.env` del servidor** — `CORS_ORIGIN_0=https://api.tudominio.com` (más el origen del cliente web si se publica), `EMAIL_URL_BASE_RECUPERACION=https://api.tudominio.com`.

---

## FASE 2 — Deploy con un comando (GitHub → Oracle)

### Task 6: Runbook — Deploy key + clonación inicial

- [ ] **Step 1: generar clave en el servidor y mostrarla**

```bash
ssh-keygen -t ed25519 -C "deploy-rentafacil-oracle" -f ~/.ssh/rentafacil_deploy -N ""
cat ~/.ssh/rentafacil_deploy.pub   # ← esta clave se la mostramos al usuario
```
En GitHub: repo `Salguero04/RentaFacilApp` → Settings → Deploy keys → "Add deploy key" → pegar la pública, **sin** write access.

- [ ] **Step 2: config SSH + clonar en `/home/ubuntu/rentafacil`**

```bash
printf "Host github.com\n  IdentityFile ~/.ssh/rentafacil_deploy\n  IdentitiesOnly yes\n" >> ~/.ssh/config
git clone git@github.com:Salguero04/RentaFacilApp.git /home/ubuntu/rentafacil
cd /home/ubuntu/rentafacil && cp .env.example .env && nano .env   # completar valores reales
```

- [ ] **Step 3: primer arranque**

```bash
cd /home/ubuntu/rentafacil && docker compose up -d --build
docker compose logs -f api   # esperado: migraciones aplicadas (InitialSqlServer → AgregarGoogleIdUsuario), seed admin, "Now listening on: http://[::]:8080"
```

### Task 7: `update.sh` en el repo

**Files:**
- Create: `update.sh` (raíz del repo — versionado, así el propio script se actualiza con `git pull`)

- [ ] **Step 1: crear `update.sh`**

```bash
#!/usr/bin/env bash
# Actualiza RentaFácil API en el servidor: git pull + rebuild + limpieza.
# Uso (desde /home/ubuntu/rentafacil):  ./update.sh
set -euo pipefail
cd "$(dirname "$0")"

echo "== 1/3 Trayendo main desde GitHub =="
git pull origin main

echo "== 2/3 Reconstruyendo y reiniciando contenedores =="
docker compose up -d --build
# Las migraciones EF se aplican solas al arrancar la API (Database.Migrate() en Program.cs)

echo "== 3/3 Limpiando imágenes huérfanas =="
docker image prune -f

docker compose ps
echo "Listo. Logs: docker compose logs -f api"
```

- [ ] **Step 2: commit** — `git add update.sh && git commit -m "feat: script de deploy update.sh (pull + rebuild + prune)"`. En el servidor, tras el siguiente pull: `chmod +x update.sh`.
- [ ] **Step 3: verificar el ciclo completo** — en el servidor: `./update.sh` → esperado: pull sin conflictos, contenedores `Up`, `docker image prune` reporta espacio liberado.

---

## FASE 3 — Correos (Brevo) y autenticación

### Task 8: `IEmailService` + implementación SMTP Brevo + config

**Files:**
- Create: `RentaFacil.API/Services/Interfaces/IEmailService.cs`
- Create: `RentaFacil.API/Services/EmailService.cs`
- Modify: `RentaFacil.API/appsettings.json` (sección `Email`)
- Modify: `RentaFacil.API/Program.cs` (DI)

**Interfaces:**
- Produces: `Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml)` — best-effort, `false` si falla (nunca lanza). `bool EstaConfigurado { get; }`.

- [ ] **Step 1: interfaz**

```csharp
namespace RentaFacil.API.Services.Interfaces;

/// <summary>
/// Envío de correos transaccionales (SMTP Brevo). Best-effort: los fallos se loguean
/// y devuelven false, nunca tumban la operación que originó el correo.
/// </summary>
public interface IEmailService
{
    bool EstaConfigurado { get; } // hay Email:Usuario/Password/Remitente
    Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
}
```

- [ ] **Step 2: implementación** (`EmailService.cs`) — lee `IConfiguration` en cada llamada (mismo patrón que `ValidadorTokenGoogle`: la API arranca sin config y `EstaConfigurado` devuelve false):

```csharp
using System.Net;
using System.Net.Mail;
using RentaFacil.API.Services.Interfaces;

namespace RentaFacil.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_configuration["Email:Usuario"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:Password"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:Remitente"]);

    public async Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        if (!EstaConfigurado) return false;
        try
        {
            using var cliente = new SmtpClient(_configuration["Email:Host"] ?? "smtp-relay.brevo.com",
                                               int.TryParse(_configuration["Email:Puerto"], out var p) ? p : 587)
            {
                EnableSsl = true, // STARTTLS en 587
                Credentials = new NetworkCredential(_configuration["Email:Usuario"], _configuration["Email:Password"])
            };
            using var mensaje = new MailMessage(_configuration["Email:Remitente"]!, destinatario, asunto, cuerpoHtml)
            {
                IsBodyHtml = true
            };
            await cliente.SendMailAsync(mensaje);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el correo a {Destinatario}", destinatario);
            return false;
        }
    }
}
```

- [ ] **Step 3: config en `appsettings.json`**

```json
"Email": {
  "Host": "smtp-relay.brevo.com",
  "Puerto": 587,
  "Usuario": "",
  "Password": "",
  "Remitente": "",
  "UrlBaseRecuperacion": ""
}
```

- [ ] **Step 4: DI en `Program.cs`** — `builder.Services.AddScoped<RentaFacil.API.Services.Interfaces.IEmailService, RentaFacil.API.Services.EmailService>();` junto a los otros services.
- [ ] **Step 5: build + commit** — `dotnet build RentaFacil.API/RentaFacil.API.csproj` → 0 errores. `git commit -m "feat: IEmailService con SMTP de Brevo (best-effort, configurable)"`.

### Task 9: Recuperación de contraseña — servicio + tests (TDD)

**Files:**
- Modify: `RentaFacil.Shared/Models/AuthDto.cs` (2 DTOs nuevos)
- Modify: `RentaFacil.API/Services/Interfaces/IAutenticacionService.cs` (+2 métodos)
- Modify: `RentaFacil.API/Services/AutenticacionService.cs` (inyectar `IEmailService`; implementar)
- Test: `RentaFacil.Tests/AutenticacionServiceTests.cs`

**Interfaces:**
- Consumes: `IEmailService.EnviarAsync` (Task 8), `IUsuarioRepository.GetByEmailAsync/UpdateAsync` (ya existen desde Google OAuth), `GenerarToken` privado existente como referencia de emisión JWT.
- Produces:
  - `Task<bool> SolicitarRecuperacionAsync(RecuperarPasswordDto dto)` — SIEMPRE devuelve `true` hacia el controller (anti-enumeración de usuarios); internamente solo envía correo si el email existe, el usuario está `Activo` y tiene email.
  - `Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto)` — valida el token de propósito `"recuperacion"`, re-hashea con BCrypt.

- [ ] **Step 1: DTOs en `AuthDto.cs`**

```csharp
public record RecuperarPasswordDto(string Email);
public record RestablecerPasswordDto(string Token, string NuevaPassword);
```

- [ ] **Step 2: tests que fallan** (mismo estilo Moq del archivo; el mock de `IEmailService` se agrega al constructor del service en los tests existentes):

```csharp
[Fact]
public async Task SolicitarRecuperacion_EmailInexistente_DevuelveTrueSinEnviarCorreo()
{
    _usuarioRepoMock.Setup(r => r.GetByEmailAsync("nadie@x.com")).ReturnsAsync((Usuario?)null);
    var resultado = await _service.SolicitarRecuperacionAsync(new RecuperarPasswordDto("nadie@x.com"));
    resultado.Should().BeTrue(); // anti-enumeración: la respuesta no revela si existe
    _emailMock.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
}

[Fact]
public async Task SolicitarRecuperacion_UsuarioActivoConEmail_EnviaCorreoConEnlace()
{
    var usuario = new Usuario { Id = 7, NombreUsuario = "dueno", Email = "dueno@x.com", Activo = true, Rol = AppRoles.Propietario };
    _usuarioRepoMock.Setup(r => r.GetByEmailAsync("dueno@x.com")).ReturnsAsync(usuario);
    _emailMock.Setup(e => e.EnviarAsync("dueno@x.com", It.IsAny<string>(), It.Is<string>(c => c.Contains("restablecer-password?token=")))).ReturnsAsync(true);

    var resultado = await _service.SolicitarRecuperacionAsync(new RecuperarPasswordDto("dueno@x.com"));

    resultado.Should().BeTrue();
    _emailMock.Verify(e => e.EnviarAsync("dueno@x.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}

[Fact]
public async Task RestablecerPassword_TokenValido_ActualizaHashYDevuelveTrue()
{
    var usuario = new Usuario { Id = 7, NombreUsuario = "dueno", Email = "dueno@x.com", Activo = true, Rol = AppRoles.Propietario };
    _usuarioRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(usuario); // agregar GetByIdAsync si no existe en el repo
    var token = _service.GenerarTokenRecuperacionParaTests(usuario); // o generar vía SolicitarRecuperacion capturando el enlace

    var ok = await _service.RestablecerPasswordAsync(new RestablecerPasswordDto(token, "NuevaClave123!"));

    ok.Should().BeTrue();
    _usuarioRepoMock.Verify(r => r.UpdateAsync(It.Is<Usuario>(u => u.PasswordHash != null && BCrypt.Net.BCrypt.Verify("NuevaClave123!", u.PasswordHash))), Times.Once);
}

[Fact]
public async Task RestablecerPassword_TokenDeLoginNormal_DevuelveFalse()
{
    // Un JWT de sesión (sin claim proposito=recuperacion) NO sirve para resetear
    var usuario = new Usuario { Id = 7, NombreUsuario = "dueno", Activo = true, Rol = AppRoles.Propietario, PasswordHash = BCrypt.Net.BCrypt.HashPassword("x") };
    _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("dueno")).ReturnsAsync(usuario);
    var login = await _service.LoginAsync(new LoginDto("dueno", "x"));

    var ok = await _service.RestablecerPasswordAsync(new RestablecerPasswordDto(login!.Token, "Hackeada123!"));

    ok.Should().BeFalse();
    _usuarioRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
}
```
Nota de diseño para el implementador: en vez del helper `GenerarTokenRecuperacionParaTests`, la opción limpia es hacer `internal string GenerarTokenRecuperacion(Usuario u)` + `InternalsVisibleTo("RentaFacil.Tests")` si el proyecto ya lo usa, o capturar el token del `cuerpoHtml` del mock con `Callback`. Elegir UNA y ser consistente.

- [ ] **Step 3: correr tests** — esperado: FAIL (métodos no existen).
- [ ] **Step 4: implementar en `AutenticacionService`**

```csharp
// Claim de propósito: distingue el token de recuperación (corto, un solo uso lógico)
// del JWT de sesión. Nunca aceptar un token sin este claim en RestablecerPassword.
private const string ClaimProposito = "proposito";
private const string PropositoRecuperacion = "recuperacion";

public async Task<bool> SolicitarRecuperacionAsync(RecuperarPasswordDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Email)) return true;
    var usuario = await _repository.GetByEmailAsync(dto.Email);
    if (usuario == null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.Email)) return true; // anti-enumeración

    var token = GenerarTokenRecuperacion(usuario); // JWT 30 min, claims: NameIdentifier + proposito=recuperacion
    var urlBase = _configuration["Email:UrlBaseRecuperacion"]?.TrimEnd('/');
    var enlace = $"{urlBase}/restablecer-password?token={Uri.EscapeDataString(token)}";
    await _emailService.EnviarAsync(usuario.Email!,
        "RentaFácil — Recuperación de contraseña",
        $"<p>Hola {usuario.NombreUsuario},</p><p>Para restablecer tu contraseña haz clic en el siguiente enlace (válido por 30 minutos):</p><p><a href=\"{enlace}\">Restablecer contraseña</a></p><p>Si no solicitaste esto, ignora este correo.</p>");
    return true;
}

public async Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto)
{
    var principal = ValidarTokenRecuperacion(dto.Token); // TokenValidationParameters iguales al AddJwtBearer + exigir claim proposito=recuperacion
    if (principal == null) return false;
    if (dto.NuevaPassword.Length < 8) return false;
    var id = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var usuario = await _repository.GetByIdAsync(id);
    if (usuario == null || !usuario.Activo) return false;
    usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
    await _repository.UpdateAsync(usuario);
    return true;
}
```
`GenerarTokenRecuperacion` reutiliza la misma clave `Jwt:Key` y el mismo `JwtSecurityTokenHandler` que `GenerarToken`, con expiración 30 min y el claim de propósito. `ValidarTokenRecuperacion` usa `JwtSecurityTokenHandler.ValidateToken` con los mismos `TokenValidationParameters` del `Program.cs` (mismos flags, `ClockSkew = TimeSpan.Zero`) y devuelve null si falta el claim o lanza. Si `IUsuarioRepository` no tiene `GetByIdAsync(int)`, agregarlo (mismo estilo de los demás métodos).

- [ ] **Step 5: correr tests** — esperado: PASS (84 + 4 nuevos).
- [ ] **Step 6: Commit** — `git commit -m "feat: recuperación de contraseña con token JWT de propósito y correo Brevo"`

### Task 10: Endpoints `recuperar-password` / `restablecer-password`

**Files:**
- Modify: `RentaFacil.API/Controllers/AuthController.cs`

**Interfaces:**
- Consumes: `SolicitarRecuperacionAsync` / `RestablecerPasswordAsync` (Task 9).

- [ ] **Step 1: agregar al controller** (mismo estilo que `login-google`):

```csharp
[HttpPost("recuperar-password")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public async Task<IActionResult> RecuperarPassword([FromBody] RecuperarPasswordDto dto)
{
    await _service.SolicitarRecuperacionAsync(dto);
    // Siempre 200: no revelamos si el email existe o no.
    return Ok(new { message = "Si el correo está registrado, recibirás un enlace de recuperación." });
}

[HttpPost("restablecer-password")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
{
    var ok = await _service.RestablecerPasswordAsync(dto);
    if (!ok) return Unauthorized(new { message = "El enlace no es válido o ya expiró. Solicita uno nuevo." });
    return NoContent();
}
```

- [ ] **Step 2: build + tests + prueba manual por Swagger** (`dotnet run --project RentaFacil.API`; POST a `recuperar-password` con email inexistente → 200 con el mensaje; `restablecer-password` con token basura → 401).
- [ ] **Step 3: Commit** — `git commit -m "feat: endpoints de recuperación y restablecimiento de contraseña"`

### Task 11: UI — "¿Olvidaste tu contraseña?" + página de restablecer

**Files:**
- Modify: `RentaFacil.UI/Pages/Login.razor` (enlace + bottom-sheet para pedir el correo)
- Create: `RentaFacil.UI/Pages/RestablecerPassword.razor` (`@page "/restablecer-password"`, lee `?token=` de la query)
- Modify: `RentaFacil.UI/Services/ApiClient.cs` (+2 métodos)

**Interfaces:**
- Consumes: endpoints de Task 10.
- Produces: `ApiClient.RecuperarPasswordAsync(string email)` → `bool` (200 = true); `ApiClient.RestablecerPasswordAsync(string token, string nuevaPassword)` → `bool` (204 = true).

- [ ] **Step 1: métodos en `ApiClient`** (mismo patrón que los POST existentes; nota: estos endpoints son anónimos, el `AuthHeaderHandler` no estorba sin token).
- [ ] **Step 2: en `Login.razor`** — enlace "¿Olvidaste tu contraseña?" bajo el botón de entrar → despliega input de email + botón enviar → llama `Api.RecuperarPasswordAsync(email)` → muestra SIEMPRE "Si el correo está registrado, recibirás un enlace" (sin diferenciar).
- [ ] **Step 3: `RestablecerPassword.razor`** — usa `LoginLayout` (mismo layout que Login); lee el token con `NavigationManager` (`new Uri(Nav.Uri).Query` + `System.Web.HttpUtility.ParseQueryString` o `QueryHelpers`); dos inputs de contraseña (nueva + confirmar, mínimo 8, deben coincidir); al enviar llama `Api.RestablecerPasswordAsync(token, password)` → éxito: mensaje + botón "Ir a iniciar sesión" (`Nav.NavigateTo("/login")`); fallo: mensaje "El enlace no es válido o ya expiró".
- [ ] **Step 4: build UI/Web + tests (sin cambios: siguen verdes) + commit** — `git commit -m "feat: UI de recuperación y restablecimiento de contraseña"`
- [ ] Nota de alcance: el enlace del correo apunta al cliente **Web** (`Email:UrlBaseRecuperacion`). En MAUI la ruta `/restablecer-password` también existe (misma RCL) pero el flujo esperado es que el usuario abra el enlace en el navegador.

### Task 12: Verificación de los cimientos Google OAuth (ya implementados)

> No hay código nuevo: los cimientos se implementaron y mergearon el 2026-07-07 (ver `decisiones.md` → "Login con Google OAuth 2.0"). Este task solo verifica, como pide la instrucción.

- [ ] **Step 1:** `dotnet ef migrations list` (desde `RentaFacil.API/`) → esperado: `AgregarGoogleIdUsuario` presente.
- [ ] **Step 2:** grep en `RentaFacil.API/Models/Usuario.cs`: `GoogleId` (`string?`, MaxLength 255) y `PasswordHash` (`string?`). Esperado: ambos.
- [ ] **Step 3:** con la API corriendo, `POST /api/auth/login-google` con `{"idToken":"x"}` sin `Google:ClientId` configurado → esperado **503** con mensaje "no está configurado".
- [ ] **Step 4:** anotar en el reporte que para ACTIVARLO faltan solo: credenciales de Google Cloud Console (`Google:ClientId` en `.env`/user-secrets) + implementación real de `IProveedorGoogle` por plataforma (hoy `ProveedorGoogleNoSoportado`, botón oculto). Eso queda FUERA de este plan.

---

## FASE 4 — Control de versiones App vs API

### Task 13: `ConfigController` + `GET /api/config/version`

**Files:**
- Create: `RentaFacil.Shared/Models/VersionAppDto.cs`
- Create: `RentaFacil.API/Controllers/ConfigController.cs`
- Modify: `RentaFacil.API/appsettings.json` (sección `VersionApp`)

**Interfaces:**
- Produces: `record VersionAppDto(string MinVersionAndroid, string LatestVersionAndroid, string UpdateUrl, bool ForceUpdate)` — el JSON serializado usa camelCase por defecto de ASP.NET (`minVersionAndroid`, ...), igual que el resto de la API, cumpliendo el shape de la instrucción.

- [ ] **Step 1: DTO** (`RentaFacil.Shared/Models/VersionAppDto.cs`):

```csharp
namespace RentaFacil.Shared.Models;

/// <summary>Política de versiones de la app móvil, servida por GET /api/config/version.</summary>
public record VersionAppDto(string MinVersionAndroid, string LatestVersionAndroid, string UpdateUrl, bool ForceUpdate);
```

- [ ] **Step 2: config** (`appsettings.json` — valores EXACTOS de la instrucción; `UpdateUrl` se sobreescribe por env en el servidor):

```json
"VersionApp": {
  "MinVersionAndroid": "1.0.1",
  "LatestVersionAndroid": "1.0.3",
  "UpdateUrl": "https://midominio.com/descargas/rentafacil.apk",
  "ForceUpdate": true
}
```

- [ ] **Step 3: controller** (`ConfigController.cs` — archivo propio, NO en `OtherControllers.cs`; sin tocar BD, lee config → no necesita Service/Repository):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaFacil.Shared.Models;

namespace RentaFacil.API.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Política de versiones de la app. Anónimo a propósito: el cliente la consulta
    /// ANTES del login (FallbackPolicy exigiría token si no).
    /// </summary>
    [HttpGet("version")]
    [AllowAnonymous]
    public ActionResult<VersionAppDto> GetVersion()
    {
        var seccion = _configuration.GetSection("VersionApp");
        return Ok(new VersionAppDto(
            seccion["MinVersionAndroid"] ?? "1.0.0",
            seccion["LatestVersionAndroid"] ?? "1.0.0",
            seccion["UpdateUrl"] ?? "",
            bool.TryParse(seccion["ForceUpdate"], out var f) && f));
    }
}
```

- [ ] **Step 4: verificar a mano** — API corriendo, `curl http://localhost:5295/api/config/version` SIN token → esperado el JSON con `minVersionAndroid` etc. (camelCase, 200 sin auth).
- [ ] **Step 5: Commit** — `git commit -m "feat: endpoint GET /api/config/version con política de versiones de la app"`

### Task 14: Comparador de versiones en Shared + tests (TDD)

**Files:**
- Create: `RentaFacil.Shared/Versionado/ComparadorVersiones.cs`
- Test: `RentaFacil.Tests/ComparadorVersionesTests.cs`

**Interfaces:**
- Produces: `static bool EsObsoleta(string? versionInstalada, string? versionMinima)` — true SOLO si ambas parsean como `System.Version` y la instalada es menor. Ante cualquier duda (null, vacío, no parseable) devuelve **false**: nunca bloquear la app por un dato malo.

- [ ] **Step 1: tests que fallan**

```csharp
using FluentAssertions;
using RentaFacil.Shared.Versionado;
using Xunit;

namespace RentaFacil.Tests;

public class ComparadorVersionesTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]   // instalada menor → obsoleta
    [InlineData("1.0.1", "1.0.1", false)]  // igual a la mínima → OK
    [InlineData("1.0.3", "1.0.1", false)]  // mayor → OK
    [InlineData("2.0", "1.9.9", false)]    // formatos de distinta longitud
    [InlineData(null, "1.0.1", false)]     // sin versión instalada → no bloquear
    [InlineData("1.0.0", null, false)]     // sin mínima → no bloquear
    [InlineData("beta", "1.0.1", false)]   // no parseable → no bloquear
    public void EsObsoleta_CasosLimite(string? instalada, string? minima, bool esperado)
        => ComparadorVersiones.EsObsoleta(instalada, minima).Should().Be(esperado);
}
```

- [ ] **Step 2: correr** → FAIL (clase no existe).
- [ ] **Step 3: implementación**

```csharp
namespace RentaFacil.Shared.Versionado;

/// <summary>
/// Compara la versión instalada de la app contra la mínima exigida por la API.
/// Conservador: ante datos nulos o no parseables devuelve false (nunca bloquea por error).
/// </summary>
public static class ComparadorVersiones
{
    public static bool EsObsoleta(string? versionInstalada, string? versionMinima)
    {
        if (!Version.TryParse(versionInstalada, out var instalada)) return false;
        if (!Version.TryParse(versionMinima, out var minima)) return false;
        return instalada < minima;
    }
}
```

- [ ] **Step 4: correr tests** → PASS. **Step 5: Commit** — `git commit -m "feat: comparador de versiones con tests de casos límite"`

### Task 15: Abstracción `IInfoVersionApp` + chequeo y bloqueo en `MainLayout`

**Files:**
- Create: `RentaFacil.UI/Abstractions/IInfoVersionApp.cs`
- Create: `RentaFacil.MAUI/Platform/MauiInfoVersionApp.cs`
- Create: `RentaFacil.Web/Platform/WebInfoVersionApp.cs`
- Modify: `RentaFacil.MAUI/MauiProgram.cs` + `RentaFacil.Web/Program.cs` (registro DI)
- Modify: `RentaFacil.UI/Services/ApiClient.cs` (`GetVersionAppAsync`)
- Modify: `RentaFacil.UI/Layout/MainLayout.razor` (chequeo + modal de bloqueo)

**Interfaces:**
- Consumes: `VersionAppDto` (Task 13), `ComparadorVersiones.EsObsoleta` (Task 14), `IDispositivoServicio.AbrirEnlaceAsync` (ya existe — se reutiliza para abrir `updateUrl`, en MAUI usa `Launcher`).
- Produces: `IInfoVersionApp { string? VersionInstalada { get; } }` — MAUI: `AppInfo.Current.VersionString`; Web: `null` (el navegador siempre carga la última versión publicada, no aplica bloqueo por APK).

- [ ] **Step 1: interfaz** (`IInfoVersionApp.cs`, estilo XML-doc español de la carpeta):

```csharp
namespace RentaFacil.UI.Abstractions;

/// <summary>
/// Versión instalada de la app según la plataforma.
/// MAUI → AppInfo.Current.VersionString; Web → null (el navegador no se "desactualiza").
/// </summary>
public interface IInfoVersionApp
{
    string? VersionInstalada { get; }
}
```

- [ ] **Step 2: implementaciones + registro** — `MauiInfoVersionApp` (`VersionInstalada => AppInfo.Current.VersionString;`, registrar Singleton en `MauiProgram.cs` junto a las otras impls de `Platform/`); `WebInfoVersionApp` (`VersionInstalada => null;`, registrar Scoped en `Web/Program.cs`).
- [ ] **Step 3: `ApiClient.GetVersionAppAsync()`** — GET `api/config/version` → `VersionAppDto?`; en `catch` devolver `null` (sin red = no bloquear).
- [ ] **Step 4: `MainLayout.razor`** — en `OnInitializedAsync` (silencioso, envuelto en try/catch):

```csharp
// Chequeo de versión: solo bloquea si la API responde, la plataforma reporta
// versión (MAUI) y la instalada es menor a la mínima con forceUpdate activo.
var info = await Api.GetVersionAppAsync();
if (info != null && info.ForceUpdate
    && ComparadorVersiones.EsObsoleta(InfoVersion.VersionInstalada, info.MinVersionAndroid))
{
    versionInfo = info;
    mostrarBloqueoVersion = true;
}
```
Markup del modal (mismo estilo overlay/modal-custom de `CrearPago.razor`), SIN botón de cerrar (bloqueante):

```razor
@if (mostrarBloqueoVersion && versionInfo != null)
{
    <div class="modal-overlay" style="z-index: 2000;"></div>
    <div class="modal-custom p-4 text-center" style="z-index: 2001;">
        <i class="bi bi-arrow-up-circle-fill text-warning" style="font-size: 3rem;"></i>
        <h4 class="fw-bold mt-2">Actualización requerida</h4>
        <p class="text-muted">Tu versión de RentaFácil ya no es compatible.
           Descarga la versión @versionInfo.LatestVersionAndroid para continuar.</p>
        <button class="btn btn-warning w-100 py-2 fw-bold rounded-pill"
                @onclick="() => Dispositivo.AbrirEnlaceAsync(versionInfo.UpdateUrl)">
            Descargar actualización
        </button>
    </div>
}
```
(Inyectar `IInfoVersionApp InfoVersion` e `IDispositivoServicio Dispositivo`; verificar que `MainLayout` ya tenga o pueda tener los estilos `.modal-overlay`/`.modal-custom` — si solo viven en páginas, copiarlos al `<style>` del layout.)

- [ ] **Step 5: builds (UI, Web, MAUI android) + tests** — `dotnet build RentaFacil.MAUI -f net10.0-android` incluido, porque se tocó `MauiProgram.cs`.
- [ ] **Step 6: Commit** — `git commit -m "feat: bloqueo de versiones obsoletas de la app vía /api/config/version"`

### Task 16: Apuntar el cliente MAUI Release al dominio de producción

**Files:**
- Modify: `RentaFacil.MAUI/Config/ApiConfig.cs:14`
- Modify: `docs/contexto/errores-conocidos.md` (entrada "La IP de producción está hardcodeada": sigue hardcodeada pero pasa de IP LAN a dominio estable — actualizar el texto)

- [ ] **Step 1:** reemplazar `http://200.126.17.232:5295` por `https://api.tudominio.com` (el dominio real de la Tarea 5; SIN puerto — Cloudflare escucha 443).
- [ ] **Step 2:** `dotnet build RentaFacil.MAUI -f net10.0-android` → 0 errores. Commit.
- [ ] **Paso manual del usuario (fuera del plan):** generar el nuevo APK Release, subir `ApplicationDisplayVersion`, y publicar el `.apk` en la URL de `updateUrl` (recomendado: GitHub Releases y poner esa URL en `VersionApp:UpdateUrl` del `.env`), siguiendo la regla de versionado de `decisiones.md`.

---

## Verificación end-to-end del plan completo

1. **Código:** `dotnet build` de API/UI/Web + `dotnet test RentaFacil.Tests` → 84 previos + ~11 nuevos, todos verdes.
2. **Servidor:** `./update.sh` corre limpio; `docker compose ps` → `api` y `sqlserver` `Up (healthy)`; logs de la API muestran migraciones aplicadas.
3. **HTTPS:** `curl -I https://api.tudominio.com/api/config/version` → 200 con JSON camelCase, certificado válido de Cloudflare; `curl -I http://...` redirige o lo maneja Cloudflare (Always Use HTTPS).
4. **CORS:** desde un origen no listado, un fetch del navegador falla por CORS; desde el origen configurado, funciona.
5. **Correo:** POST `recuperar-password` con el email del admin (con Brevo configurado en `.env`) → llega correo real → el enlace abre `/restablecer-password?token=...` en el cliente web → cambiar contraseña → login con la nueva funciona, con la vieja da 401.
6. **Versionado:** bajar temporalmente `ApplicationDisplayVersion` del MAUI a `1.0.0` en un build de prueba → al abrir la app aparece el modal bloqueante y el botón abre `updateUrl`; con `1.0.3` la app entra normal; el cliente Web nunca se bloquea.
7. **Regresión:** login normal, CRUD y tiempo real (SignalR entre dos clientes) siguen funcionando contra el servidor de producción.
8. **Docs (regla de cierre de CLAUDE.md):** actualizar "Último Contexto" + `arquitectura.md` ("Lo que NO existe": quitar "No hay Docker en uso real"), `errores-conocidos.md` (CORS/HTTPS → ya RESUELTO; IP hardcodeada → texto nuevo), `decisiones.md` (nuevas entradas: Docker/Oracle/Cloudflare, Brevo, versionado de app) y `flujo-de-trabajo.md` (la Fase de deploy "Oracle Cloud" pasa de planeada a ACTUAL, con `update.sh` documentado).

## Fuera de alcance (explícito)

- Activar Google login end-to-end (credenciales + `IProveedorGoogle` real) — solo se verifica el cimiento (Task 12).
- CI/CD automático (GitHub Actions): el flujo pedido es manual por SSH con `update.sh`.
- Notificaciones push, gráficas de Ingresos y demás backlog de `CLAUDE.md`.
- Migrar el motor de BD: SQL Server se mantiene (si el GATE de la Task 4 falla, se consulta al usuario el fallback, no se decide solo).
