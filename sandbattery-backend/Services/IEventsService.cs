using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IEventsService
{
    Task<EventList> GetEventsAsync(string productKey, DateTime? from, DateTime? to, string[]? types, string? source, int limit, int offset);
    Task<AlertList> GetActiveAlertsAsync(string productKey);
    Task<Alert?> AcknowledgeAlertAsync(int alertId, string productKey);
    Task AddHeartbeatAsync(Heartbeat heartbeat);
}
