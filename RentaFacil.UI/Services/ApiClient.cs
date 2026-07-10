using System.Net.Http.Json;
using RentaFacil.Shared.Models;

namespace RentaFacil.UI.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public HttpClient HttpClient => _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<InquilinoDto>> GetInquilinosAsync()
    {
        return await _http.GetFromJsonAsync<List<InquilinoDto>>("api/inquilinos") ?? new List<InquilinoDto>();
    }

    public async Task<InquilinoDto?> GetInquilinoByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<InquilinoDto>($"api/inquilinos/{id}");
    }

    public async Task<InquilinoDto?> CrearInquilinoAsync(CrearInquilinoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/inquilinos", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InquilinoDto>() : null;
    }

    public async Task<bool> UpdateInquilinoAsync(int id, CrearInquilinoDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/inquilinos/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteInquilinoAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/inquilinos/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<InmuebleDto>> GetInmueblesAsync() => await _http.GetFromJsonAsync<List<InmuebleDto>>("api/inmuebles") ?? new();

    public async Task<InmuebleDto?> GetInmuebleByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<InmuebleDto>($"api/inmuebles/{id}");
    }

    public async Task<InmuebleDto?> CrearInmuebleAsync(CrearInmuebleDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/inmuebles", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InmuebleDto>() : null;
    }

    public async Task<bool> UpdateInmuebleAsync(int id, CrearInmuebleDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/inmuebles/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteInmuebleAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/inmuebles/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ContratoDto>> GetContratosAsync() => await _http.GetFromJsonAsync<List<ContratoDto>>("api/contratos") ?? new();
    public async Task<ContratoDto?> CrearContratoAsync(CrearContratoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/contratos", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ContratoDto>() : null;
    }

    public async Task<bool> ActualizarContratoAsync(int id, CrearContratoDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/contratos/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteContratoAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/contratos/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PagoDto>> GetPagosAsync() => await _http.GetFromJsonAsync<List<PagoDto>>("api/pagos") ?? new();
    public async Task<PagoDto?> CrearPagoAsync(CrearPagoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/pagos", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagoDto>() : null;
    }

    public async Task<bool> ActualizarPagoAsync(int id, CrearPagoDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/pagos/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UnidadDto>> GetUnidadesAsync() => await _http.GetFromJsonAsync<List<UnidadDto>>("api/unidades") ?? new();

    public async Task<UnidadDto?> CrearUnidadAsync(CrearUnidadDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/unidades", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UnidadDto>() : null;
    }

    public async Task<bool> UpdateUnidadAsync(int id, CrearUnidadDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/unidades/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUnidadAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/unidades/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Medidores ───────────────────────────────────────────────────
    public async Task<List<MedidorDto>> GetMedidoresAsync() => await _http.GetFromJsonAsync<List<MedidorDto>>("api/medidores") ?? new();

    public async Task<MedidorDto?> CrearMedidorAsync(CrearMedidorDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/medidores", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MedidorDto>() : null;
    }

    public async Task<bool> UpdateMedidorAsync(int id, CrearMedidorDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/medidores/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMedidorAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/medidores/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<MedidorInquilinoDto>> GetVinculosMedidorAsync(int medidorId)
        => await _http.GetFromJsonAsync<List<MedidorInquilinoDto>>($"api/medidores/{medidorId}/inquilinos") ?? new();

    public async Task<MedidorInquilinoDto?> VincularInquilinoAsync(CrearMedidorInquilinoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/medidores/inquilinos", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MedidorInquilinoDto>() : null;
    }

    public async Task<bool> ActualizarVinculoAsync(int id, CrearMedidorInquilinoDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/medidores/inquilinos/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DesvincularInquilinoAsync(int vinculoId)
    {
        var response = await _http.DeleteAsync($"api/medidores/inquilinos/{vinculoId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<FacturaMedidorDto>> GetFacturasMedidorAsync(int medidorId)
        => await _http.GetFromJsonAsync<List<FacturaMedidorDto>>($"api/medidores/{medidorId}/facturas") ?? new();

    public async Task<FacturaMedidorDto?> GuardarFacturaMedidorAsync(CrearFacturaMedidorDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/medidores/facturas", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<FacturaMedidorDto>() : null;
    }

    public async Task<bool> DeleteFacturaMedidorAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/medidores/facturas/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ResumenMedidorDto>> GetResumenMedidoresAsync(int mes, int anio)
        => await _http.GetFromJsonAsync<List<ResumenMedidorDto>>($"api/medidores/resumen?mes={mes}&anio={anio}") ?? new();

    public async Task<List<DetalleServicioPagoDto>> GetCobrosContratoAsync(int contratoId, int mes, int anio)
        => await _http.GetFromJsonAsync<List<DetalleServicioPagoDto>>($"api/medidores/cobros?contratoId={contratoId}&mes={mes}&anio={anio}") ?? new();

    public async Task<List<RecordatorioDto>> GetRecordatoriosAsync() => await _http.GetFromJsonAsync<List<RecordatorioDto>>("api/recordatorios") ?? new();

    public async Task<RecordatorioDto?> CrearRecordatorioAsync(CrearRecordatorioDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/recordatorios", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<RecordatorioDto>() : null;
    }

    public async Task<bool> UpdateRecordatorioAsync(int id, CrearRecordatorioDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/recordatorios/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRecordatorioAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/recordatorios/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Portal del inquilino (vinculación, api/mi/*, reportes de pago) ─────
    public async Task<CodigoVinculacionDto?> GenerarCodigoVinculacionAsync(int contratoId)
    {
        var response = await _http.PostAsync($"api/contratos/{contratoId}/codigo-vinculacion", null);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<CodigoVinculacionDto>() : null;
    }

    // El endpoint de QR exige Bearer (a propósito: NO se hizo [AllowAnonymous] solo para que
    // sirva un <img src> directo). Se descarga con el HttpClient autenticado y la UI lo muestra
    // como data URL (data:image/png;base64,...).
    public async Task<byte[]?> GetQrPngAsync(string codigo)
    {
        var response = await _http.GetAsync($"api/contratos/codigo-vinculacion/{codigo}/qr");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<(LoginResultDto? Resultado, string? Error)> RegistrarInquilinoAsync(RegistrarInquilinoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/registrar-inquilino", dto);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<LoginResultDto>(), null);
        }

        var error = await response.Content.ReadFromJsonAsync<MensajeErrorDto>();
        return (null, error?.Message ?? "No se pudo completar el registro.");
    }

    public async Task<List<MiContratoDto>> GetMisContratosAsync()
        => await _http.GetFromJsonAsync<List<MiContratoDto>>("api/mi/contratos") ?? new();

    public async Task<List<MiPagoDto>> GetMisPagosAsync()
        => await _http.GetFromJsonAsync<List<MiPagoDto>>("api/mi/pagos") ?? new();

    public async Task<List<MiConsumoDto>> GetMisConsumosAsync()
        => await _http.GetFromJsonAsync<List<MiConsumoDto>>("api/mi/consumos") ?? new();

    public async Task<List<MiNotificacionDto>> GetMisNotificacionesAsync()
        => await _http.GetFromJsonAsync<List<MiNotificacionDto>>("api/mi/notificaciones") ?? new();

    public async Task<bool> MarcarNotificacionLeidaAsync(int id)
    {
        var response = await _http.PutAsync($"api/mi/notificaciones/{id}/leida", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VincularCodigoAsync(string codigo)
    {
        var response = await _http.PostAsJsonAsync("api/mi/vincular", new VincularCodigoDto(codigo));
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetMiReciboPdfAsync(int pagoId, string formato)
    {
        var response = await _http.GetAsync($"api/mi/pagos/{pagoId}/recibo?formato={formato}");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<ReportePagoDto?> CrearReportePagoAsync(CrearReportePagoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/mi/reportes-pago", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ReportePagoDto>() : null;
    }

    public async Task<List<ReportePagoDto>> GetMisReportesPagoAsync()
        => await _http.GetFromJsonAsync<List<ReportePagoDto>>("api/mi/reportes-pago") ?? new();

    // ── Bandeja del arrendador para reportes de pago ("ya pagué") ──────────
    public async Task<List<ReportePagoDto>> GetReportesPagoAsync()
        => await _http.GetFromJsonAsync<List<ReportePagoDto>>("api/reportes-pago") ?? new();

    public async Task<bool> ConfirmarReporteAsync(int id)
    {
        var response = await _http.PutAsync($"api/reportes-pago/{id}/confirmar", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RechazarReporteAsync(int id)
    {
        var response = await _http.PutAsync($"api/reportes-pago/{id}/rechazar", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetComprobanteAsync(int reporteId)
    {
        var response = await _http.GetAsync($"api/reportes-pago/{reporteId}/comprobante");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    // Forma local del body `{message}` que devuelven los 400 de la API (no hay un DTO
    // compartido para errores genéricos en RentaFacil.Shared).
    private record MensajeErrorDto(string Message);
}
