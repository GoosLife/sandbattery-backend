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

    public async Task<SensorMeasurement?> GetLatestMeasurementAsync(string productKey)
    {
        var entity = await _db.SensorMeasurements
            .Where(m => m.ProductKey == productKey)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<DataHistory> GetMeasurementHistoryAsync(
        string productKey, DateTime from, DateTime to, MeasurementInterval? interval, int limit)
    {
        var entities = await _db.SensorMeasurements
            .Where(m => m.ProductKey == productKey && m.Timestamp >= from && m.Timestamp <= to)
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

    public async Task<SensorMeasurement> AddMeasurementAsync(string productKey, SensorMeasurement dto)
    {
        var settings = await _db.Settings.FirstOrDefaultAsync(s => s.ProductKey == productKey)
            ?? new SettingsEntity { ProductKey = productKey };

        var status = DetermineStatus(dto, settings);

        var entity = new SensorMeasurementEntity
        {
            ProductKey = productKey,
            Timestamp = DateTime.Parse(dto.Timestamp, null, DateTimeStyles.RoundtripKind),
            SandTemp = dto.SandTemp,
            WaterTempIn = dto.WaterTempIn,
            WaterTempOut = dto.WaterTempOut,
            FlowRate = dto.FlowRate,
            PowerW = dto.PowerW,
            EnergyKwh = dto.EnergyKwh,
            Status = status
        };

        _db.SensorMeasurements.Add(entity);

        if (status == "ERROR")
        {
            _db.Alerts.Add(new AlertEntity
            {
                ProductKey = productKey,
                Severity = "WARNING",
                Type = "SENSOR_ERROR",
                Message = "En eller flere sensorer rapporterer ugyldige værdier (mulig sensorafkobling).",
                Timestamp = DateTime.UtcNow,
                Acknowledged = false
            });
        }
        else if (status == "CRITICAL")
        {
            _db.Alerts.Add(new AlertEntity
            {
                ProductKey = productKey,
                Severity = "CRITICAL",
                Type = "TEMP_LIMIT_EXCEEDED",
                Message = $"Maks temperaturen er overskredet ({dto.SandTemp:F1}\u00b0C). Sandbatteriet nedkøles.",
                Timestamp = DateTime.UtcNow,
                Acknowledged = false
            });
        }

        await _db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public static string DetermineStatus(SensorMeasurement m, SettingsEntity s)
    {
        if (m.SandTemp <= -126 || m.WaterTempIn <= -126 || m.WaterTempOut <= -126)
            return "ERROR";

        if (m.SandTemp >= s.MaxSandTemp)
            return "CRITICAL";

        if (m.SandTemp < 0)
            return "WARNING";

        return "OK";
    }


    private static SensorMeasurement MapToDto(SensorMeasurementEntity e) => new()
    {
        Timestamp = e.Timestamp.ToString("o"),
        ProductKey = e.ProductKey,
        SandTemp = e.SandTemp,
        WaterTempIn = e.WaterTempIn,
        WaterTempOut = e.WaterTempOut,
        FlowRate = e.FlowRate,
        PowerW = e.PowerW,
        EnergyKwh = e.EnergyKwh,
        Status = e.Status
    };
}
