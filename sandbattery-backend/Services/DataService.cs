using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

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
        string productKey, DateTime from, DateTime to, string? interval, int limit)
    {
        var entities = await _db.SensorMeasurements
            .Where(m => m.ProductKey == productKey && m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (interval is not null)
        {
            var span = ParseInterval(interval);
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
            Interval = interval,
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

        if (status == "CRITICAL")
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

        if (m.SandTemp >= s.MaxSandTemp * 0.9f)
            return "WARNING";

        return "OK";
    }

    private static TimeSpan ParseInterval(string interval) => interval switch
    {
        "1m"  => TimeSpan.FromMinutes(1),
        "5m"  => TimeSpan.FromMinutes(5),
        "15m" => TimeSpan.FromMinutes(15),
        "30m" => TimeSpan.FromMinutes(30),
        "1h"  => TimeSpan.FromHours(1),
        "6h"  => TimeSpan.FromHours(6),
        "1d"  => TimeSpan.FromDays(1),
        _     => TimeSpan.FromMinutes(1)
    };

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
