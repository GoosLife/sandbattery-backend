using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class EventsService : IEventsService
{
    private readonly SandbatteryDbContext _db;

    public EventsService(SandbatteryDbContext db) => _db = db;

    public async Task<EventList> GetEventsAsync(
        string productKey, DateTime? from, DateTime? to,
        string[]? types, string? source, int limit, int offset)
    {
        var toDate = to ?? DateTime.UtcNow;

        var query = _db.Events
            .Where(e => e.ProductKey == productKey && e.Timestamp <= toDate);

        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);

        if (types is { Length: > 0 })
            query = query.Where(e => types.Contains(e.Type));

        if (source is not null)
            query = query.Where(e => e.Source == source);

        var total = await query.CountAsync();
        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return new EventList
        {
            Total = total,
            Limit = limit,
            Offset = offset,
            Events = events.Select(MapToDto).ToList()
        };
    }

    public async Task<AlertList> GetActiveAlertsAsync(string productKey)
    {
        var alerts = await _db.Alerts
            .Where(a => a.ProductKey == productKey && !a.Acknowledged)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return new AlertList
        {
            Count = alerts.Count,
            Alerts = alerts.Select(MapToDto).ToList()
        };
    }

    public async Task<Alert?> AcknowledgeAlertAsync(int alertId, string productKey)
    {
        var entity = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.ProductKey == productKey);

        if (entity is null) return null;

        entity.Acknowledged = true;
        await _db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task AddHeartbeatAsync(Heartbeat dto)
    {
        _db.Heartbeats.Add(new HeartbeatEntity
        {
            ProductKey = dto.ProductKey,
            Timestamp = DateTime.Parse(dto.Timestamp, null, DateTimeStyles.RoundtripKind),
            UptimeSeconds = dto.UptimeSeconds
        });
        await _db.SaveChangesAsync();
    }

    private static Models.Event MapToDto(EventEntity e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        Source = e.Source,
        Timestamp = e.Timestamp.ToString("o"),
        Description = e.Description
    };

    private static Alert MapToDto(AlertEntity a) => new()
    {
        Id = a.Id,
        Severity = a.Severity,
        Type = a.Type,
        Message = a.Message,
        Timestamp = a.Timestamp.ToString("o"),
        Acknowledged = a.Acknowledged
    };
}
