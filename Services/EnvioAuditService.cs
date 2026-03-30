using Microsoft.EntityFrameworkCore;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;
using static Tracker.Helpers.Constants;

namespace Tracker.Services;

public interface IEnvioAuditService
{
    Task AuditarEnvioAsync(EnvioAudit envioAudit);

    Task<(IEnumerable<EnvioAuditDTO>, int RecordsTotal)> ObtenerAuditoriaEnvioAsync(FiltroAuditoriaDTO filtro);

    Task AuditarEnviosAsync(List<EnvioAudit> auditorias);
}

public class EnvioAuditService : IEnvioAuditService
{
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<Tracker_DevelContext> _contextFactory;
    private readonly Tracker_DevelContext _context;

    public EnvioAuditService(
        IConfiguration configuration,
        IDbContextFactory<Tracker_DevelContext> contextFactory,
        Tracker_DevelContext context)
    {
        _configuration = configuration;
        _contextFactory = contextFactory;
        _context = context;
    }


    public async Task AuditarEnviosAsync(List<EnvioAudit> auditorias)
    {
        try
        {
            if (auditorias == null || auditorias.Count == 0)
                return;

            using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var a in auditorias)
            {
                // 🔥 evita tracking de navegación
                a.Estado = null;
                a.Id = 0;
            }

            await context.EnviosAudit.AddRangeAsync(auditorias);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Error.WriteLog($"ERROR AUDITORIA BULK: {ex.Message}");
        }
    }


    public async Task AuditarEnvioAsync(EnvioAudit envioAudit)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var auditSetting = _configuration.GetSection("AuditarEnvios").Get<AuditSettingDTO>();

            if (auditSetting != null)
            {
                if (!auditSetting.SoloErrores || envioAudit.EstadoId == (int)eEnviosEstados.ConError)
                {
                    context.EnviosAudit.Add(envioAudit);
                }

                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM EnviosAudit WHERE Fecha <= GETDATE() - {0}",
                    auditSetting.LimpiarAlosDias);
            }
            else
            {
                context.EnviosAudit.Add(envioAudit);
            }

            await context.SaveChangesAsync(); // ✔ aislado
        }
        catch (Exception ex)
        {
            // ⚠️ nunca romper el flujo principal
            Error.WriteLog($"ERROR AUDITORIA: {ex.Message}");
        }
    }

    public async Task<(IEnumerable<EnvioAuditDTO>, int RecordsTotal)> ObtenerAuditoriaEnvioAsync(FiltroAuditoriaDTO filtro)
    {
        var query = _context.EnviosAudit
            .Include(i => i.Estado)
            .AsNoTracking()
            .AsQueryable();

        if (filtro.Numero.HasValue)
            query = query.Where(q => q.Envio == filtro.Numero);

        if (filtro.GuiaNumero.HasValue)
            query = query.Where(q => q.Guia == filtro.GuiaNumero);

        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            query = query.Where(q => q.Usuario.Contains(filtro.Usuario));

        if (filtro.EstadoId.HasValue && filtro.EstadoId > 0)
            query = query.Where(q => q.EstadoId == filtro.EstadoId);

        if (DateTime.TryParse(filtro.Desde, out var desde))
            query = query.Where(q => q.Fecha >= desde.Date);

        if (DateTime.TryParse(filtro.Hasta, out var hasta))
            query = query.Where(q => q.Fecha < hasta.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filtro.SearchValue))
        {
            var search = filtro.SearchValue.Trim();
            query = query.Where(q =>
                q.Envio.ToString().Contains(search) ||
                q.Guia.ToString().Contains(search) ||
                q.Usuario.Contains(search) ||
                q.Observacion.Contains(search) ||
                (q.Direccion ?? "").Contains(search) ||
                (q.Estado == null ? "" : q.Estado.Descripcion).Contains(search)
            );
        }

        var total = await query.CountAsync();

        bool asc = (filtro.SortDirection?.ToLower() == "asc");

        query = filtro.SortColumn?.ToLower() switch
        {
            "envio" => asc ? query.OrderBy(x => x.Envio) : query.OrderByDescending(x => x.Envio),
            "guia" => asc ? query.OrderBy(x => x.Guia) : query.OrderByDescending(x => x.Guia),
            "usuario" => asc ? query.OrderBy(x => x.Usuario) : query.OrderByDescending(x => x.Usuario),
            "estado" => asc ? query.OrderBy(x => (x.Estado == null ? "" : x.Estado.Descripcion)) : query.OrderByDescending(x => (x.Estado == null ? "" : x.Estado.Descripcion)),
            _ => asc ? query.OrderBy(x => x.Fecha) : query.OrderByDescending(x => x.Fecha),
        };

        var data = await query
            .Skip(filtro.Skip)
            .Take(filtro.PageSize)
            .Select(s => new EnvioAuditDTO
            {
                Id = s.Id,
                Envio = s.Envio,
                Guia = s.Guia,
                Fecha = s.Fecha,
                Usuario = s.Usuario,
                Estado = (s.Estado == null ? "" : s.Estado.Descripcion),
                EstadoColor = (s.Estado == null ? "" : s.Estado.Color),
                Observacion = s.Observacion,
                Direccion = s.Direccion,
                CodigoViaje = s.CodigoViaje
            })
            .ToListAsync();

        return (data, total);
    }
}