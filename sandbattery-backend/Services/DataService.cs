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

    public DataService(SandbatteryDbContext db) => _db = db;

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

        var entity = new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = DateTime.Parse(dto.Timestamp, null, DateTimeStyles.RoundtripKind),
            PowerW = dto.PowerW,
            EnergyKwh = dto.EnergyKwh,
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

        if (status == "ERROR")
        {
            _db.Alerts.Add(new AlertEntity
            {
                DeviceId = deviceId,
                Severity = "WARNING",
                Type = "SENSOR_ERROR",
                Message = "En eller flere sensorer rapporterer ugyldige værdier (mulig sensorafkobling).",
                Timestamp = DateTime.UtcNow,
                Acknowledged = false
            });
        }
        else if (status == "CRITICAL")
        {
            var sandTemp = SandTempReading(dto);
            _db.Alerts.Add(new AlertEntity
            {
                DeviceId = deviceId,
                Severity = "CRITICAL",
                Type = "TEMP_LIMIT_EXCEEDED",
                Message = $"Maks temperaturen er overskredet ({sandTemp?.Value:F1}°C). Sandbatteriet nedkøles.",
                Timestamp = DateTime.UtcNow,
                Acknowledged = false
            });
        }

        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public static string DetermineStatus(SensorMeasurement m, SettingsEntity s)
    {
        if (m.Temperatures.Any(t => t.Value <= -126) || m.FlowRates.Any(f => f.Value <= -126))
            return "ERROR";

        var sandTemp = SandTempReading(m);
        if (sandTemp is null) return "OK";

        if (sandTemp.Value >= s.MaxSandTemp) return "CRITICAL";
        if (sandTemp.Value < 0) return "WARNING";

        return "OK";
    }

    private static TemperatureReading? SandTempReading(SensorMeasurement m) =>
        m.Temperatures.FirstOrDefault(t => t.Label.Equals("sand", StringComparison.OrdinalIgnoreCase))
        ?? m.Temperatures.MinBy(t => t.Index);

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
        EnergyKwh = e.EnergyKwh,
        Status = e.Status
    };
}
