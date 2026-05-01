using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class AutomationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(IServiceScopeFactory scopeFactory, ILogger<AutomationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunAsync(stoppingToken);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SandbatteryDbContext>();
        var control = scope.ServiceProvider.GetRequiredService<IControlService>();

        var devices = await db.Devices.ToListAsync(ct);
        foreach (var device in devices)
        {
            try
            {
                await ProcessDeviceAsync(db, control, device.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automation error for device {DeviceId}", device.Id);
            }
        }
    }

    private static async Task ProcessDeviceAsync(
        SandbatteryDbContext db, IControlService control, int deviceId, CancellationToken ct)
    {
        var settings = await db.Settings
            .FirstOrDefaultAsync(s => s.DeviceId == deviceId, ct)
            ?? new SettingsEntity { DeviceId = deviceId };

        var recentReadings = await db.SensorMeasurements
            .Include(m => m.TemperatureReadings)
            .Where(m => m.DeviceId == deviceId && m.Timestamp >= DateTime.UtcNow.AddSeconds(-30))
            .ToListAsync(ct);

        if (recentReadings.Count == 0) return;

        var allReadings = recentReadings
            .SelectMany(m => m.TemperatureReadings)
            .GroupBy(t => t.SensorIndex);

        foreach (var group in allReadings)
        {
            if (group.All(t => t.Value == -127))
            {
                var alertType = $"SENSOR_OFFLINE_{group.Key}";
                var hasAlert = await db.Alerts
                    .AnyAsync(a => a.DeviceId == deviceId && a.Type == alertType && !a.Acknowledged, ct);

                if (!hasAlert)
                {
                    db.Alerts.Add(new AlertEntity
                    {
                        DeviceId = deviceId,
                        Severity = "WARNING",
                        Type = alertType,
                        Message = $"Sensor {group.Key} rapporterer -127°C i over 30 sekunder.",
                        Timestamp = DateTime.UtcNow,
                        Acknowledged = false
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);

        var latest = await db.SensorMeasurements
            .Include(m => m.TemperatureReadings)
            .Where(m => m.DeviceId == deviceId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (latest is null || !settings.AutoHeatingEnabled) return;

        var sandTemp = latest.TemperatureReadings
            .FirstOrDefault(t => t.Label.Equals("sand", StringComparison.OrdinalIgnoreCase))
            ?? latest.TemperatureReadings.MinBy(t => t.SensorIndex);

        if (sandTemp is null) return;

        var now = DateTime.UtcNow;
        var currentHourUtc = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        // Get current electricity price (DK2 area)
        var currentPrice = await db.PriceEntries
            .Join(db.ElectricityPrices.Where(ep => ep.Area == "DK2"),
                pe => pe.ElectricityPriceId,
                ep => ep.Id,
                (pe, _) => pe)
            .Where(pe => pe.Hour == currentHourUtc)
            .Select(pe => pe.PriceDkkKwh)
            .FirstOrDefaultAsync(ct);

        var heaters = await db.ActuatorStatuses
            .Where(a => a.DeviceId == deviceId && a.Actuator == "heater")
            .ToListAsync(ct);

        if (sandTemp.Value >= settings.MaxSandTemp)
        {
            // Safety net: temp at limit → ensure auto-activated heaters are off
            foreach (var heater in heaters.Where(h => h.Active && h.Source == CommandSource.rule.ToString()))
                await control.ControlHeaterAsync(deviceId, heater.ActuatorIndex, HeaterAction.off, CommandSource.rule);
        }
        else if (currentPrice > 0 && currentPrice < settings.PriceLimitDkk)
        {
            // Cheap electricity + temp below limit → auto turn on heaters
            foreach (var heater in heaters.Where(h => !h.Active))
                await control.ControlHeaterAsync(deviceId, heater.ActuatorIndex, HeaterAction.on, CommandSource.rule);
        }
    }
}
