using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class ControlService : IControlService
{
    private readonly SandbatteryDbContext _db;

    public ControlService(SandbatteryDbContext db) => _db = db;

    public async Task<SystemStatus> GetSystemStatusAsync(int deviceId)
    {
        var heaters = await _db.ActuatorStatuses
            .Where(a => a.DeviceId == deviceId && a.Actuator == "heater")
            .OrderBy(a => a.ActuatorIndex)
            .ToListAsync();

        var pump = await _db.ActuatorStatuses
            .FirstOrDefaultAsync(a => a.DeviceId == deviceId && a.Actuator == "pump");

        var now = DateTime.UtcNow.ToString("o");
        return new SystemStatus
        {
            Heaters = heaters.Count > 0
                ? heaters.Select(MapToDto).ToList()
                : [new ActuatorStatus { Index = 0, LastChanged = now }],
            Pump = pump is null ? new ActuatorStatus { LastChanged = now } : MapToDto(pump)
        };
    }

    public async Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlPumpAsync(
        int deviceId, PumpAction action, CommandSource source)
    {
        var active = action == PumpAction.start;
        var ev = await UpdateActuatorAndLogEvent(deviceId, "pump", 0, active, source);

        return (true, new ControlCommandResponse
        {
            Success = true,
            Action = action.ToString(),
            Source = source.ToString(),
            Timestamp = ev.Timestamp.ToString("o"),
            EventId = ev.Id
        }, false);
    }

    public async Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlHeaterAsync(
        int deviceId, int heaterIndex, HeaterAction action, CommandSource source)
    {
        if (action == HeaterAction.on)
        {
            var latest = await _db.SensorMeasurements
                .Include(m => m.TemperatureReadings)
                .Where(m => m.DeviceId == deviceId)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            var settings = await _db.Settings
                .FirstOrDefaultAsync(s => s.DeviceId == deviceId)
                ?? new SettingsEntity { DeviceId = deviceId };

            if (latest is not null)
            {
                var sandTemp = latest.TemperatureReadings
                    .FirstOrDefault(t => t.Label.Equals("sand", StringComparison.OrdinalIgnoreCase))
                    ?? latest.TemperatureReadings.MinBy(t => t.SensorIndex);

                if (sandTemp is not null && sandTemp.Value >= settings.MaxSandTemp)
                    return (false, null, true);
            }
        }

        var active = action == HeaterAction.on;
        var ev = await UpdateActuatorAndLogEvent(deviceId, "heater", heaterIndex, active, source);

        return (true, new ControlCommandResponse
        {
            Success = true,
            Action = action.ToString(),
            Source = source.ToString(),
            Timestamp = ev.Timestamp.ToString("o"),
            EventId = ev.Id
        }, false);
    }

    private async Task<EventEntity> UpdateActuatorAndLogEvent(
        int deviceId, string actuator, int actuatorIndex, bool active, CommandSource source)
    {
        var status = await _db.ActuatorStatuses
            .FirstOrDefaultAsync(a => a.DeviceId == deviceId && a.Actuator == actuator && a.ActuatorIndex == actuatorIndex);

        if (status is null)
        {
            status = new ActuatorStatusEntity { DeviceId = deviceId, Actuator = actuator, ActuatorIndex = actuatorIndex };
            _db.ActuatorStatuses.Add(status);
        }

        status.Active = active;
        status.Source = source.ToString();
        status.LastChanged = DateTime.UtcNow;

        var eventType = (actuator, active) switch
        {
            ("pump", true)   => "pump_start",
            ("pump", false)  => "pump_stop",
            ("heater", true) => "heat_on",
            _                => "heat_off"
        };

        var description = (actuator, active) switch
        {
            ("pump", true)   => "Vandpumpe startet",
            ("pump", false)  => "Vandpumpe stoppet",
            ("heater", true) => $"Varmelegeme {actuatorIndex} aktiveret",
            _                => $"Varmelegeme {actuatorIndex} deaktiveret"
        };

        var ev = new EventEntity
        {
            DeviceId = deviceId,
            Type = eventType,
            Source = source.ToString(),
            Timestamp = DateTime.UtcNow,
            Description = description
        };
        _db.Events.Add(ev);

        await _db.SaveChangesAsync();
        return ev;
    }

    private static ActuatorStatus MapToDto(ActuatorStatusEntity e) => new()
    {
        Index = e.ActuatorIndex,
        Active = e.Active,
        Source = e.Source,
        LastChanged = e.LastChanged.ToString("o")
    };
}
