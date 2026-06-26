using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RentaFacil.API.Models;

namespace RentaFacil.API.Data;

public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AplicarAuditoria(DbContext? context)
    {
        if (context == null) return;

        var usuarioId = ObtenerUsuarioIdActual();
        var ahora = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreadoPorId = usuarioId;
                entry.Entity.FechaCreacion = ahora;
                entry.Entity.ModificadoPorId = usuarioId;
                entry.Entity.FechaModificacion = ahora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModificadoPorId = usuarioId;
                entry.Entity.FechaModificacion = ahora;
            }
        }
    }

    private int? ObtenerUsuarioIdActual()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;
        var valor = usuario?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out var id) ? id : null;
    }
}
