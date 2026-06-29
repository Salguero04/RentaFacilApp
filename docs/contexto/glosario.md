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
- **Servicio incluido** → un servicio básico (agua, luz) que un `Contrato` incluye, además de la renta. Cada servicio tiene un `TipoServicio` (`Agua`/`Luz`/`Otro`; Gas excluido a propósito) y una `ModalidadServicio`. El total que paga el inquilino = renta + servicios.
- **Modalidad de servicio** → `MontoFijo` (compartido): el inquilino paga un monto fijo del contrato; el arrendador registra la **planilla** real y asume la diferencia (la app la reporta como pérdida/ganancia). `PorConsumo` (individual): el inquilino paga su consumo real capturado cada periodo; pass-through, el arrendador no entra, sin pérdida.
- **Planilla / Costo de servicio** → el monto REAL que paga el arrendador por un servicio compartido (`MontoFijo`), registrado por inmueble + tipo + mes/año en la sección **Medidores** de `Ingresos.razor`. Entidad `CostoServicio`.
- **Medidores** → subsección de `Ingresos.razor` que muestra, por inmueble y tipo de servicio: **Cobrado** a inquilinos / **Planilla** real / **Neto** (pérdida en rojo, ganancia en verde). Para servicios por consumo muestra solo lo cobrado (pass-through). Endpoint `GET /api/servicios/resumen?mes&anio`.

## Entidades principales (en `RentaFacil.API/Models/`)
- **Inquilino** → `Id`, `NombreCompleto`, `Identificacion` (DNI/CI/RUC), `Telefono?`, `FotoUrl?`, `FechaRegistro`, `UsuarioId`. Tiene muchos `Contrato`s (borrado restringido: no se puede borrar un Inquilino con contratos).
- **Inmueble** → `Id`, `Nombre`, `Direccion`, `Tipo` (`TipoInmueble`: Unico/Multiple), `MontoRenta` (solo relevante si `Tipo == Unico`), `UsuarioId`. Tiene muchas `Unidad`es (borrado en cascada).
- **Unidad** → `Id`, `Nombre`, `MontoRenta`, `Ocupada`, `InmuebleId` (FK, cascada al borrar el Inmueble).
- **Contrato** → `Id`, `InquilinoId` (FK, restringido), `UnidadId` (FK, restringido), `Monto`, `Garantia`, `Frecuencia`, `DuracionMeses`, `DiaPago`, `FechaInicio`, `FechaFin`, `Observaciones?`, `Activo`. Tiene muchos `Pago`s (borrado en cascada).
- **Pago** → `Id`, `ContratoId` (FK, cascada), `TotalMonto`, `ACuenta`, `Servicios` (suma de los servicios cobrados), `FechaPago`, `Periodo`, `Facturado`, `Completado`. Tiene muchos `DetalleServicioPago` (cascada).
- **Recordatorio** → `Id`, `InquilinoId` (FK, cascada), `ContratoId?` (sin FK estricta, solo referencia), `Detalle`, `FechaProgramada`, `UsuarioId`.
- **ServicioContrato** → `Id`, `ContratoId` (FK, cascada), `Tipo` (`TipoServicio`), `Modalidad` (`ModalidadServicio`), `MontoFijo` (aplica si `MontoFijo`), `Activo`, `UsuarioId`. Servicios incluidos en un contrato (puede tener varios).
- **CostoServicio** → `Id`, `InmuebleId` (FK, cascada), `Tipo`, `Mes`, `Anio`, `MontoReal`, `FechaRegistro`, `UsuarioId`. La planilla real por inmueble/tipo/periodo (upsert por esa clave).
- **DetalleServicioPago** → `Id`, `PagoId` (FK, cascada), `Tipo`, `Monto`, `UsuarioId`. Desglose de cuánto pagó el inquilino por cada servicio en un pago (para reportar agua vs luz y para itemizar el recibo).

## Siglas y nombres internos
- **DTO** → Data Transfer Object; en este repo siempre un `record` en `RentaFacil.Shared/Models/`, con el patrón `Crear{Entidad}Dto` / `{Entidad}Dto`.
- **MAUI** → .NET Multi-platform App UI, el framework del cliente (`RentaFacil.MAUI`).
- **Blazor Hybrid** → el modo de MAUI que renderiza páginas `.razor` (HTML/CSS/C#) dentro de la app nativa, en vez de UI nativa por plataforma.
- **`ApiConfig.LOCAL`** → compile constant que decide la URL base de la API que usa el cliente (ver `decisiones.md`).
- **QuestPDF** → librería usada por `ReciboService` para generar los PDFs de recibos.
- **`Other*` (OtherControllers.cs, OtherServices.cs, OtherRepositories.cs)** → nombre interno (no es un término de dominio) para los archivos que agrupan Contrato/Pago/Unidad en un solo archivo en vez de uno por entidad.
