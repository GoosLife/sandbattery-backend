using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IEventsService
{
    Task<EventList> GetEventsAsync(int deviceId, DateTime? from, DateTime? to, string[]? types, string? source, int limit, int offset);
    Task<AlertList> GetActiveAlertsAsync(int deviceId);
    Task<Alert?> AcknowledgeAlertAsync(int alertId, int deviceId);
    Task AddHeartbeatAsync(int deviceId, Heartbeat heartbeat);
}
