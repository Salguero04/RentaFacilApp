using RentaFacil.Shared.Enums;

namespace RentaFacil.Shared.Models;

// Código de vinculación generado por el arrendador para un contrato: se muestra como QR
// para que el inquilino cree su cuenta (o vincule un contrato adicional) escaneándolo.
public record CodigoVinculacionDto(string Codigo, DateTime FechaExpiracion);

// DTOs del portal del inquilino (api/mi/*) — vista de solo lectura de sus propios
// contratos/pagos/consumos/notificaciones, derivada de los Inquilino vinculados a su cuenta.
public record MiContratoDto(int ContratoId, string NombreArrendador, string NombreUnidad, string NombreInmueble,
                            decimal Monto, FrecuenciaPago Frecuencia, int DiaPago, DateTime FechaInicio, DateTime FechaFin, bool Activo);

public record MiPagoDto(int PagoId, int ContratoId, string Periodo, decimal TotalMonto, decimal ACuenta,
                        decimal Servicios, DateTime FechaPago, bool Completado);

public record MiConsumoDto(string NombreMedidor, TipoServicio Tipo, decimal LecturaAnterior, decimal LecturaActual,
                           MetodoCobroInquilino MetodoCobro);

public record MiNotificacionDto(int Id, string Tipo, string? Detalle, DateTime Fecha, bool Notificado);

// Body de POST api/mi/vincular — el inquilino ya logueado agrega otro contrato/arrendador con un código nuevo.
public record VincularCodigoDto(string Codigo);
