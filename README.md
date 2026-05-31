# 🏢 RentaFácil App

**Estado del proyecto: 🚧 En desarrollo 🚧**

RentaFácil es una plataforma integral multiplataforma diseñada para la gestión eficiente de propiedades en alquiler. Esta aplicación busca simplificar el control de bienes raíces tanto para arrendadores como para administradores, centralizando todas las operaciones en un solo lugar.

## 🎯 Características Principales (En progreso)

- 🏠 **Gestión de Inmuebles:** Administración de propiedades únicas o edificios con múltiples unidades (departamentos, locales, oficinas).
- 👥 **Control de Inquilinos:** Base de datos con información detallada de los arrendatarios.
- 📄 **Contratos Inteligentes:** Registro de contratos con control de vigencias, montos de renta, garantías y frecuencias de pago (mensual, quincenal, semanal).
- 💰 **Pagos y Cobranzas:** Seguimiento preciso de los pagos realizados, saldos pendientes y servicios extra.
- 🧾 **Generación de Recibos:** Creación automática de recibos en formato PDF (estilo Ticket y Carta) listos para compartir o imprimir.

## 💻 Tecnologías Utilizadas

Este proyecto se basa en una arquitectura limpia dividida en Cliente, Servidor y Recursos Compartidos:

- **Frontend (Cliente):** `.NET MAUI Blazor Hybrid` - Ofrece una experiencia nativa en iOS, Android, Mac y Windows, reutilizando componentes web (HTML, CSS, C#).
- **Backend (Servidor):** `ASP.NET Core Web API` - API robusta y segura para gestionar la lógica de negocio.
- **Base de Datos:** `Entity Framework Core` con `MySQL` (Producción) / `SQLite` (Desarrollo local).
- **Reportes:** `QuestPDF` para la generación ágil de documentos.

## 📁 Estructura del Repositorio

- `RentaFacil.Shared/`: Modelos de datos compartidos (DTOs, Enums).
- `RentaFacil.API/`: Proyecto Backend y lógica de acceso a datos.
- `RentaFacil.MAUI/`: Proyecto Frontend (Aplicación móvil/de escritorio).
- `betas APKs/`: Directorio donde se almacenan las versiones compiladas de prueba (APK) para Android.

---
*Desarrollado por [Salguero04](https://github.com/Salguero04)*
