using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class ControlService : IControlService
{
    private readonly SandbatteryDbContext _db;

    public ControlService(SandbatteryDbContext db) => _db = db;

    public async Task<SystemStatus> GetSystemStatusAsync(string productKey)
    {
        var heater = await _db.ActuatorStatuses
            .FirstOrDefaultAsync(a => a.ProductKey == productKey && a.Actuator == "heater");
        var pump = await _db.ActuatorStatuses
            .FirstOrDefaultAsync(a => a.ProductKey == productKey && a.Actuator == "pump");

        var now = DateTime.UtcNow.ToString("o");
        return new SystemStatus
        {
            Heater = heater is null ? new ActuatorStatus { LastChanged = now } : MapToDto(heater),
            Pump = pump is null ? new ActuatorStatus { LastChanged = now } : MapToDto(pump)
        };
    }

    public async Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlPumpAsync(
        string productKey, string action, string source)
    {
        var active = action == "start";
        var ev = await UpdateActuatorAndLogEvent(productKey, "pump", active, source);

        return (true, new ControlCommandResponse
        {
            Success = true,
            Action = action,
            Source = source,
            Timestamp = ev.Timestamp.ToString("o"),
            EventId = ev.Id
        }, false);
    }

    public async Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlHeaterAsync(
        string productKey, string action, string source)
    {
        if (action == "on")
        {
            var latest = await _db.SensorMeasurements
                .Where(m => m.ProductKey == productKey)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            var settings = await _db.Settings
                .FirstOrDefaultAsync(s => s.ProductKey == productKey)
                ?? new SettingsEntity { ProductKey = productKey };

            if (latest is not null && latest.SandTemp >= settings.MaxSandTemp)
                return (false, null, true);
        }

        var active = action == "on";
        var ev = await UpdateActuatorAndLogEvent(productKey, "heater", active, source);

        return (true, new ControlCommandResponse
        {
            Success = true,
            Action = action,
            Source = source,
            Timestamp = ev.Timestamp.ToString("o"),
            EventId = ev.Id
        }, false);
    }

    private async Task<EventEntity> UpdateActuatorAndLogEvent(
        string productKey, string actuator, bool active, string source)
    {
        var status = await _db.ActuatorStatuses
            .FirstOrDefaultAsync(a => a.ProductKey == productKey && a.Actuator == actuator);

        if (status is null)
        {
            status = new ActuatorStatusEntity { ProductKey = productKey, Actuator = actuator };
            _db.ActuatorStatuses.Add(status);
        }

        status.Active = active;
        status.Source = source;
        status.LastChanged = DateTime.UtcNow;

        var eventType = (actuator, active) switch
        {
            ("pump", true) => "pump_start",
            ("pump", false) => "pump_stop",
            ("heater", true) => "heat_on",
            _ => "heat_off"
        };

        var description = (actuator, active) switch
        {
            ("pump", true) => "Vandpumpe startet",
            ("pump", false) => "Vandpumpe stoppet",
            ("heater", true) => "Varmelegeme aktiveret",
            _ => "Varmelegeme deaktiveret"
        };

        var ev = new EventEntity
        {
            ProductKey = productKey,
            Type = eventType,
            Source = source,
            Timestamp = DateTime.UtcNow,
            Description = description
        };
        _db.Events.Add(ev);

        await _db.SaveChangesAsync();
        return ev;
    }

    private static ActuatorStatus MapToDto(ActuatorStatusEntity e) => new()
    {
        Active = e.Active,
        Source = e.Source,
        LastChanged = e.LastChanged.ToString("o")
    };
}
