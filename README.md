# 🏢 RentaFácil App

**Estado del proyecto: 🚧 En desarrollo (Fase 1 — MVP local) 🚧**

RentaFácil es una plataforma multiplataforma para la gestión de propiedades en alquiler. Centraliza el control de inmuebles, inquilinos, contratos, pagos y la emisión de recibos, tanto para arrendadores como administradores.

## 🎯 Características

- 🏠 **Gestión de Inmuebles:** propiedades únicas o edificios con múltiples unidades (departamentos, locales, oficinas).
- 👥 **Control de Inquilinos:** base de datos con la información de los arrendatarios.
- 📄 **Contratos:** vigencias, montos de renta, garantías y frecuencias de pago (mensual, quincenal, semanal), con cálculo automático de fecha fin.
- 💰 **Pagos y Cobranzas:** seguimiento de pagos, saldos pendientes y servicios extra.
- 🧾 **Recibos PDF:** generación automática en formato Ticket (80mm) y Carta (A4) con QuestPDF.

## 💻 Tecnologías

Arquitectura limpia dividida en Cliente, Servidor y Recursos Compartidos:

- **Frontend (Cliente):** `.NET MAUI Blazor Hybrid` — experiencia nativa en Android, iOS, Windows y macOS reutilizando componentes web (HTML/CSS/C#), más uso web.
- **Backend (Servidor):** `ASP.NET Core Web API` (.NET 10).
- **Base de Datos:** `Entity Framework Core` con `SQL Server` (local y producción), organizada en schemas `auth`/`renta`/`config`/`audit`.
- **Reportes:** `QuestPDF` para la generación de documentos.
- **Pruebas:** `xUnit` + `Moq` + `FluentAssertions`.

## 📁 Estructura del repositorio

- `RentaFacil.Shared/` — Modelos de datos compartidos (DTOs, Enums).
- `RentaFacil.API/` — Backend, lógica de negocio y acceso a datos.
- `RentaFacil.MAUI/` — Frontend (app móvil/escritorio/web).
- `RentaFacil.Tests/` — Pruebas unitarias.
- `betas APKs/` — Versiones compiladas de prueba (APK) para Android.

## 📚 Documentación

La documentación técnica del proyecto está organizada para consumo de [Claude Code](https://claude.com/claude-code):

- [`CLAUDE.md`](CLAUDE.md) — índice central: arranque rápido, pendientes y enlaces.
- [`docs/contexto/`](docs/contexto/) — contexto detallado por eje (arquitectura, convenciones, decisiones, glosario, flujo de trabajo, errores conocidos).

## 🚀 Cómo empezar

```bash
# Compilar todo
dotnet build RentaFacil.slnx

# Levantar la API (http://0.0.0.0:5295)
dotnet run --project RentaFacil.API

# Ejecutar pruebas
dotnet test RentaFacil.Tests
```

---
*Desarrollado por [Salguero04](https://github.com/Salguero04)*
