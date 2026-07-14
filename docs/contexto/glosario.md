# Glosario y entidades

## Términos del dominio
- **Arrendador** → el usuario propietario de la app (hoy un único usuario implícito; `UsuarioId` en las entidades referencia a este rol, aunque todavía no se filtra por él — ver `errores-conocidos.md`).
- **Inmueble Único** → propiedad independiente (casa, departamento) sin subdivisiones; usa `Inmueble.MontoRenta` directamente.
- **Inmueble Múltiple** → propiedad que se subdivide en `Unidad`es (edificio, complejo, galería); el monto de renta vive en cada `Unidad`, no en el `Inmueble`.
- **Unidad** → subdivisión de un Inmueble Múltiple (ej. "Depto 101", "Oficina B"), con su propio `MontoRenta` y estado `Ocupada`.
- **Contrato** → acuerdo de arrendamiento que liga un `Inquilino` con una `Unidad`, con `Monto`, `Garantia`, `Frecuencia` de pago, `DuracionMeses`, `DiaPago`, `FechaInicio`/`FechaFin` (esta última calculada automáticamente) y estado `Activo`.
- **Pago** → registro de un cobro sobre un `Contrato` en un `Periodo` dado, con `TotalMonto`, `ACuenta` (lo abonado), `Servicios` (extras como luz/agua), y los flags `Facturado`/`Completado`.
- **Periodo** → identificador textual del ciclo de pago facturado, ej. `"MAY-JUN/26"` o `"MAY-26"` — no es un tipo de dato estructurado, es un `string` libre.
- **Saldo** → concepto derivado (no un campo persistido): `TotalMonto - ACuenta` de un `Pago`.
- **Facturado** → si el inquilino ya recibió el recibo/factura de ese pago (no si el pago está completo — eso es `Completado`).
- **Completado** → si el `Pago` está cubierto al 100% (`ACuenta >= TotalMonto`).
- **Frecuencia de pago** → enum `FrecuenciaPago` (`Mensual`, `Quincenal`, `Semanal`), define cada cuánto vence la renta de un Contrato.
- **Recibo Ticket / Recibo Carta** → los dos formatos de PDF que genera `ReciboService` con QuestPDF: Ticket (80mm, pensado para impresora térmica) y Carta (A4, formato formal). El endpoint es `GET /api/pagos/{id}/recibo/{formato}` con `formato` = `ticket` | `carta` (default `carta`).
- **Estado de Pagos** → vista principal de la app (`Home.razor`, ruta `/`) que muestra, por inquilino y periodo, el estado de cobro con indicadores de color. Semántica de colores — **ya implementada y verificada** en `Home.razor.GetEstadoColor` (ver "Último Contexto" de `CLAUDE.md`, tarea 2026-06-27): 🔵 azul = fecha de pago próxima; 🟡 amarillo = la fecha de pago es hoy; 🔴 rojo = fecha de pago ya pasó (vencido); barra 🟢 verde = pago `Completado`; badge verde/rojo = factura entregada / sin entregar (`Facturado`). `Pagos.razor` (ruta `/pagos`, sin entrada en el menú) es una vista secundaria en tabla con los mismos indicadores; `DetallePagos.razor` muestra el historial de pagos por contrato.
- **Ingresos** → vista (`Ingresos.razor`) de reporte mensual por inmueble (alquileres + servicios), con selector de mes/año.
- **Recordatorio** → nota libre con fecha programada, ligada a un `Inquilino` (y opcionalmente a un `Contrato`), creada desde el bottom-sheet "Recordatorio" de `Home.razor`. Es solo una nota persistida (`GET`/`POST`/`DELETE /api/recordatorios`) — no dispara notificaciones push ni recordatorios automáticos (eso sigue en "Pendiente" de `CLAUDE.md`).
- **Medidor** → entidad propia (pantalla `Medidores.razor`, ruta `/medidores`) que representa un servicio físico (agua, luz) de un `Inmueble`. Tiene `TipoServicio` (`Agua`/`Electricidad`/`Gas`/`Otro`; Gas en el enum pero sin uso real — bombona personal en Ecuador) y `ModoMedidor` (`PorLectura`: el arrendador lee su propio sub-medidor; `PorPlanilla`: llega la factura de la empresa y se reparte). Uno o varios `Inquilino`s se vinculan a un medidor (`MedidorInquilino`).
- **Vínculo medidor↔inquilino (`MedidorInquilino`)** → liga un `Inquilino` (y opcionalmente un `ContratoId`, informativo) a un `Medidor`, con sus lecturas (`LecturaAnterior`/`LecturaActual`) y `MetodoCobroInquilino`: `Tarifa` (consumo × `Medidor.Tarifa`), `Prorrateo` (reparte la `FacturaMedidor` del periodo entre todos los vínculos en prorrateo de ese medidor, proporcional al consumo), `Manual` (monto fijo `MontoFijo` ingresado por el arrendador).
- **Factura / Planilla (`FacturaMedidor`)** → el monto REAL que paga el arrendador por un `Medidor` en un mes/año dado (upsert por esa clave). Se compara contra lo cobrado a los inquilinos vinculados para reportar pérdida/ganancia cuando el método de cobro es `Prorrateo`.
- **Sección Medidores de Ingresos** → en `Ingresos.razor`, muestra por medidor: **Cobrado** a inquilinos / **Planilla** real / **Neto** (pérdida en rojo, ganancia en verde). Endpoint `GET /api/medidores/resumen?mes&anio`. `CrearPago` consulta `GET /api/medidores/cobros?contratoId&mes&anio` para precargar los servicios a cobrar de ese contrato.
- **Notificación pendiente (`NotificacionPendiente`)** → registro que deja `ContratoService.UpdateAsync` al editar un contrato (`Tipo="ContratoEditado"`). Desde 2026-07-14 **sí tiene consumidor**: el portal del inquilino la lista y la marca leída (`GET/PUT api/mi/notificaciones`). No dispara push ni email todavía.
- **Portal del inquilino** → conjunto de pantallas `/mi*` (`MiPortal`, `MisPagos`, `MisConsumos`, `MisNotificaciones`, `ReportarPago`) y endpoints `api/mi/*` (`MiPortalController`, rol `Inquilino`) donde el inquilino ve SOLO su data, derivada de la cadena cuenta→inquilinos→contratos del token.
- **Cuenta de inquilino** → `Usuario` con rol `Inquilino`, creada por registro self-service (`/registro-inquilino` + `POST api/auth/registrar-inquilino`) con un código de vinculación vigente. Se liga a la persona `Inquilino` vía `Inquilino.UsuarioCuentaId`.
- **Código de vinculación (`CodigoVinculacion`)** → código de 8 caracteres (sin 0/O/1/I) que el arrendador genera por contrato y muestra como **QR** (o texto para escribir a mano); expira a los 7 días y es de un solo uso (reclamo atómico). Es el "secreto compartido" que autoriza el registro/vinculación del inquilino.
- **Reporte de pago (`ReportePago`)** → "ya pagué" del inquilino (monto, comentario, foto de comprobante opcional ≤1MB) con estado `Pendiente`/`Confirmado`/`Rechazado`. El arrendador lo gestiona desde la bandeja `/reportes-pago` (con refresco SignalR en vivo); confirmar NO crea el `Pago` — el arrendador lo registra en CrearPago.

## Entidades principales (en `RentaFacil.API/Models/`)
- **Inquilino** → `Id`, `NombreCompleto`, `Identificacion` (DNI/CI/RUC), `Telefono?`, `FotoUrl?`, `FechaRegistro`, `UsuarioId`, `UsuarioCuentaId?` (cuenta del inquilino en `auth.Usuarios`; null = sin registrarse). Tiene muchos `Contrato`s (borrado restringido: no se puede borrar un Inquilino con contratos).
- **Inmueble** → `Id`, `Nombre`, `Direccion`, `Tipo` (`TipoInmueble`: Unico/Multiple), `MontoRenta` (solo relevante si `Tipo == Unico`), `UsuarioId`. Tiene muchas `Unidad`es (borrado en cascada).
- **Unidad** → `Id`, `Nombre`, `MontoRenta`, `Ocupada`, `InmuebleId` (FK, cascada al borrar el Inmueble).
- **Contrato** → `Id`, `InquilinoId` (FK, restringido), `UnidadId` (FK, restringido), `Monto`, `Garantia`, `Frecuencia`, `DuracionMeses`, `DiaPago`, `FechaInicio`, `FechaFin`, `Observaciones?`, `Activo`. Tiene muchos `Pago`s (borrado en cascada).
- **Pago** → `Id`, `ContratoId` (FK, cascada), `TotalMonto`, `ACuenta`, `Servicios` (suma de los servicios cobrados), `FechaPago`, `Periodo`, `Facturado`, `Completado`. Tiene muchos `DetalleServicioPago` (cascada).
- **Recordatorio** → `Id`, `InquilinoId` (FK, cascada), `ContratoId?` (sin FK estricta, solo referencia), `Detalle`, `FechaProgramada`, `UsuarioId`.
- **Medidor** → `Id`, `Nombre`, `Tipo` (`TipoServicio`), `InmuebleId` (FK, cascada), `Modo` (`ModoMedidor`), `SubConsumoHabilitado`, `Tarifa`, `Activo`, `UsuarioId`. Tiene muchos `MedidorInquilino` y `FacturaMedidor` (cascada).
- **MedidorInquilino** → `Id`, `MedidorId` (FK, cascada), `InquilinoId` (FK, restringido), `ContratoId?` (sin FK estricta), `MetodoCobro` (`MetodoCobroInquilino`), `MontoFijo`, `LecturaAnterior`, `LecturaActual`, `Activo`, `UsuarioId`.
- **FacturaMedidor** → `Id`, `MedidorId` (FK, cascada), `Mes`, `Anio`, `MontoReal`, `FechaRegistro`, `UsuarioId`. La planilla real por medidor/periodo (upsert por `MedidorId+Mes+Anio`).
- **DetalleServicioPago** → `Id`, `PagoId` (FK, cascada), `Tipo`, `Monto`, `UsuarioId`. Desglose de cuánto pagó el inquilino por cada servicio en un pago (para reportar agua vs luz y para itemizar el recibo).
- **NotificacionPendiente** → `Id`, `ContratoId`, `InquilinoId`, `Tipo` (string, ej. `"ContratoEditado"`), `Detalle?`, `Fecha`, `Notificado`, `UsuarioId`. Sin FK estricta a Contrato/Inquilino.
- **CodigoVinculacion** → `Id`, `Codigo` (8 chars, índice único), `ContratoId`, `InquilinoId` (sin FK estricta), `FechaCreacion`, `FechaExpiracion` (+7 días), `UsadoEn?` (null = disponible), `UsuarioId` (arrendador).
- **ReportePago** → `Id`, `ContratoId`, `InquilinoId` (sin FK estricta), `Monto` decimal(18,2), `Comentario?` (500), `FotoComprobante?` (varbinary ≤1MB validado en API), `FechaReporte`, `Estado` (`EstadoReportePago`), `UsuarioId` (arrendador dueño), `CuentaInquilinoId` (cuenta que reportó).

## Siglas y nombres internos
- **DTO** → Data Transfer Object; en este repo siempre un `record` en `RentaFacil.Shared/Models/`, con el patrón `Crear{Entidad}Dto` / `{Entidad}Dto`.
- **MAUI** → .NET Multi-platform App UI, el framework del cliente (`RentaFacil.MAUI`).
- **Blazor Hybrid** → el modo de MAUI que renderiza páginas `.razor` (HTML/CSS/C#) dentro de la app nativa, en vez de UI nativa por plataforma.
- **`ApiConfig.LOCAL`** → compile constant que decide la URL base de la API que usa el cliente (ver `decisiones.md`).
- **QuestPDF** → librería usada por `ReciboService` para generar los PDFs de recibos.
- **`Other*` (OtherControllers.cs, OtherServices.cs, OtherRepositories.cs)** → nombre interno (no es un término de dominio) para los archivos que agrupan Contrato/Pago/Unidad en un solo archivo en vez de uno por entidad.
