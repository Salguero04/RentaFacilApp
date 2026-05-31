using System.Net.Http.Json;
using RentaFacil.Shared.Models;

namespace RentaFacil.MAUI.Services;

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

    public async Task<List<PagoDto>> GetPagosAsync() => await _http.GetFromJsonAsync<List<PagoDto>>("api/pagos") ?? new();
    public async Task<PagoDto?> CrearPagoAsync(CrearPagoDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/pagos", dto);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagoDto>() : null;
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
}
