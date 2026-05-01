using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;
using static sandbattery_backend.Models.MeasurementIntervalExtensions;

namespace sandbattery_backend.Services;

public class DataService : IDataService
{
    private readonly SandbatteryDbContext _db;
    private readonly IControlService _control;

    public DataService(SandbatteryDbContext db, IControlService control)
    {
        _db = db;
        _control = control;
    }

    public async Task<SensorMeasurement?> GetLatestMeasurementAsync(int deviceId)
    {
        var entity = await _db.SensorMeasurements
            .Include(m => m.TemperatureReadings)
            .Include(m => m.FlowRateReadings)
            .Where(m => m.DeviceId == deviceId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<DataHistory> GetMeasurementHistoryAsync(
        int deviceId, DateTime from, DateTime to, MeasurementInterval? interval, int limit)
    {
        var entities = await _db.SensorMeasurements
            .Include(m => m.TemperatureReadings)
            .Include(m => m.FlowRateReadings)
            .Where(m => m.DeviceId == deviceId && m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (interval is not null)
        {
            var span = interval.Value.ToTimeSpan();
            var sampled = new List<SensorMeasurementEntity>();
            DateTime? lastTs = null;
            foreach (var e in entities)
            {
                if (lastTs is null || e.Timestamp - lastTs.Value >= span)
                {
                    sampled.Add(e);
                    lastTs = e.Timestamp;
                }
            }
            entities = sampled;
        }

        var page = entities.Take(limit).ToList();
        return new DataHistory
        {
            From = from.ToString("o"),
            To = to.ToString("o"),
            Interval = interval?.ToApiString(),
            Count = page.Count,
            Data = page.Select(MapToDto).ToList()
        };
    }

    public async Task<SensorMeasurement> AddMeasurementAsync(int deviceId, SensorMeasurement dto)
    {
        var settings = await _db.Settings.FirstOrDefaultAsync(s => s.DeviceId == deviceId)
            ?? new SettingsEntity { DeviceId = deviceId };

        var status = DetermineStatus(dto, settings);
        var sandTemp = SandTempReading(dto);

        var entity = new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = ResolveTimestamp(dto.Timestamp),
            PowerW = dto.PowerW,
            Status = status,
            TemperatureReadings = dto.Temperatures.Select(t => new TemperatureSensorReadingEntity
            {
                SensorIndex = t.Index,
                Label = t.Label,
                Value = t.Value
            }).ToList(),
            FlowRateReadings = dto.FlowRates.Select(f => new FlowRateSensorReadingEntity
            {
                SensorIndex = f.Index,
                Value = f.Value
            }).ToList()
        };

        _db.SensorMeasurements.Add(entity);

        switch (status)
        {
            case "ERROR":
                var cutoff = DateTime.UtcNow.AddSeconds(-30);
                var recentMeasurements = await _db.SensorMeasurements
                    .Include(m => m.TemperatureReadings)
                    .Where(m => m.DeviceId == deviceId && m.Timestamp >= cutoff)
                    .ToListAsync();

                foreach (var t in dto.Temperatures.Where(t => t.Value <= -126))
                {
                    var allBad = recentMeasurements
                        .All(m => m.TemperatureReadings
                            .Any(r => r.SensorIndex == t.Index && r.Value <= -126));

                    if (!allBad) continue;

                    var type = $"SENSOR_OFFLINE_{t.Index}";
                    var hasAlert = await _db.Alerts
                        .AnyAsync(a => a.DeviceId == deviceId && a.Type == type && !a.Acknowledged);
                    if (!hasAlert)
                        _db.Alerts.Add(new AlertEntity
                        {
                            DeviceId = deviceId,
                            Severity = "ERROR",
                            Type = type,
                            Message = $"Temperaturmåler {t.Label} (index {t.Index}) ikke forbundet.",
                            Timestamp = DateTime.UtcNow,
                            Acknowledged = false
                        });
                }
                break;

            case "WARNING":
                var hasWarnAlert = await _db.Alerts
                    .AnyAsync(a => a.DeviceId == deviceId && a.Type == "TEMP_LOW" && !a.Acknowledged);
                if (!hasWarnAlert)
                    _db.Alerts.Add(new AlertEntity
                    {
                        DeviceId = deviceId,
                        Severity = "WARNING",
                        Type = "TEMP_LOW",
                        Message = "Temperaturen er meget lav – tjek om målingerne passer.",
                        Timestamp = DateTime.UtcNow,
                        Acknowledged = false
                    });
                break;
            case "CRITICAL":
                var hasCritAlert = await _db.Alerts
                    .AnyAsync(a => a.DeviceId == deviceId && a.Type == "TEMP_LIMIT_EXCEEDED" && !a.Acknowledged);
                if (!hasCritAlert)
                    _db.Alerts.Add(new AlertEntity
                    {
                        DeviceId = deviceId,
                        Severity = "CRITICAL",
                        Type = "TEMP_LIMIT_EXCEEDED",
                        Message = $"Maks temperatur overskredet ({sandTemp?.Value:F1}°C). Sandbatteriet nedkøles.",
                        Timestamp = DateTime.UtcNow,
                        Acknowledged = false
                    });
                break;
        }

        await _db.SaveChangesAsync();

        if (status == "CRITICAL")
        {
            // Force-start pump (safety rule: temp > max → pump ON until temp ≤ max)
            var pump = await _db.ActuatorStatuses
                .FirstOrDefaultAsync(a => a.DeviceId == deviceId && a.Actuator == "pump");
            if (pump is null || !pump.Active)
                await _control.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.rule);

            // Shut off all active heaters (temp > max → heater BLOCKED)
            var activeHeaters = await _db.ActuatorStatuses
                .Where(a => a.DeviceId == deviceId && a.Actuator == "heater" && a.Active)
                .ToListAsync();
            foreach (var h in activeHeaters)
                await _control.ControlHeaterAsync(deviceId, h.ActuatorIndex, HeaterAction.off, CommandSource.rule);
        }

        // Per-sensor flow offline check (pump ON + sensor reads 0 for 30s)
        var pumpOn = await _db.ActuatorStatuses
            .AnyAsync(a => a.DeviceId == deviceId && a.Actuator == "pump" && a.Active);

        if (pumpOn && dto.FlowRates.Count > 0)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-30);
            var recentMeasurements = await _db.SensorMeasurements
                .Include(m => m.FlowRateReadings)
                .Where(m => m.DeviceId == deviceId && m.Timestamp >= cutoff)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            foreach (var flow in dto.FlowRates)
            {
                if (flow.Value != 0) continue;

                var allBad = recentMeasurements.All(m => m.FlowRateReadings
                        .Any(f => f.SensorIndex == flow.Index && f.Value == 0));

                if (!allBad) continue;

                var type = $"FLOW_SENSOR_OFFLINE_{flow.Index}";
                var hasAlert = await _db.Alerts
                    .AnyAsync(a => a.DeviceId == deviceId && a.Type == type && !a.Acknowledged);

                if (!hasAlert)
                    _db.Alerts.Add(new AlertEntity
                    {
                        DeviceId = deviceId,
                        Severity = "WARNING",
                        Type = type,
                        Message = $"Flow sensor (index {flow.Index}) registrerer ingen gennemstrømning mens pumpen kører.",
                        Timestamp = DateTime.UtcNow,
                        Acknowledged = false
                    });
            }

            await _db.SaveChangesAsync();
        }

        return MapToDto(entity);
    }

    public static string DetermineStatus(SensorMeasurement m, SettingsEntity s)
    {
        if (m.Temperatures.Any(t => t.Value <= -126) || m.FlowRates.Any(f => f.Value <= -126))
            return "ERROR";

        var sandTemp = SandTempReading(m);
        if (sandTemp is null) return "OK";

        if (sandTemp.Value >= s.MaxSandTemp) return "CRITICAL";
        if (sandTemp.Value >= s.MaxSandTemp * 0.9f) return "WARNING";
        if (sandTemp.Value < 0) return "WARNING";

        return "OK";
    }

    private static TemperatureReading? SandTempReading(SensorMeasurement m) =>
        m.Temperatures.FirstOrDefault(t => t.Label.Equals("sand", StringComparison.OrdinalIgnoreCase))
        ?? m.Temperatures.MinBy(t => t.Index);

    public async Task<EnergyReading?> GetLatestEnergyAsync(int deviceId)
    {
        var entity = await _db.EnergyReadings
            .Where(e => e.DeviceId == deviceId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();

        return entity is null ? null : MapEnergyToDto(entity);
    }

    public async Task<EnergyHistory> GetEnergyHistoryAsync(int deviceId, DateTime from, DateTime to, int limit)
    {
        var entities = await _db.EnergyReadings
            .Where(e => e.DeviceId == deviceId && e.Timestamp >= from && e.Timestamp <= to)
            .OrderBy(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();

        return new EnergyHistory
        {
            From = from.ToString("o"),
            To = to.ToString("o"),
            Count = entities.Count,
            Data = entities.Select(MapEnergyToDto).ToList()
        };
    }

    public async Task<EnergyReading> AddEnergyReadingAsync(int deviceId, EnergyReading dto)
    {
        var entity = new EnergyReadingEntity
        {
            DeviceId = deviceId,
            Timestamp = ResolveTimestamp(dto.Timestamp),
            EnergyKwh = dto.EnergyKwh
        };

        _db.EnergyReadings.Add(entity);
        await _db.SaveChangesAsync();
        return MapEnergyToDto(entity);
    }

    private static DateTime ResolveTimestamp(string raw)
    {
        if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var parsed))
        {
            var utc = parsed.ToUniversalTime();
            if (Math.Abs((utc - DateTime.UtcNow).TotalSeconds) <= 30)
                return utc;
        }
        return DateTime.UtcNow;
    }

    private static EnergyReading MapEnergyToDto(EnergyReadingEntity e) => new()
    {
        Timestamp = e.Timestamp.ToString("o"),
        EnergyKwh = e.EnergyKwh
    };

    private static SensorMeasurement MapToDto(SensorMeasurementEntity e) => new()
    {
        Timestamp = e.Timestamp.ToString("o"),
        Temperatures = e.TemperatureReadings.OrderBy(t => t.SensorIndex).Select(t => new TemperatureReading
        {
            Index = t.SensorIndex,
            Label = t.Label,
            Value = t.Value
        }).ToList(),    
        FlowRates = e.FlowRateReadings.OrderBy(f => f.SensorIndex).Select(f => new FlowRateReading
        {
            Index = f.SensorIndex,
            Value = f.Value
        }).ToList(),
        PowerW = e.PowerW,
        Status = e.Status
    };
}
