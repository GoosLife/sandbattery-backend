using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class SettingsService : ISettingsService
{
    private readonly SandbatteryDbContext _db;

    public SettingsService(SandbatteryDbContext db) => _db = db;

    public async Task<DeviceSettings> GetSettingsAsync(string productKey)
    {
        var entity = await _db.Settings.FirstOrDefaultAsync(s => s.ProductKey == productKey);
        return entity is null ? new DeviceSettings() : MapToDto(entity);
    }

    public async Task<(bool Success, List<string> UpdatedFields)> UpdateSettingsAsync(
        string productKey, SettingsUpdateRequest request)
    {
        var entity = await _db.Settings.FirstOrDefaultAsync(s => s.ProductKey == productKey);

        if (entity is null)
        {
            entity = new SettingsEntity { ProductKey = productKey };
            _db.Settings.Add(entity);
        }

        var updated = new List<string>();

        if (request.MaxSandTemp.HasValue)        { entity.MaxSandTemp = request.MaxSandTemp.Value;               updated.Add("max_sand_temp"); }
        if (request.MinPumpTemp.HasValue)        { entity.MinPumpTemp = request.MinPumpTemp.Value;               updated.Add("min_pump_temp"); }
        if (request.PumpIntervalSeconds.HasValue){ entity.PumpIntervalSeconds = request.PumpIntervalSeconds.Value; updated.Add("pump_interval_seconds"); }
        if (request.PriceLimitDkk.HasValue)      { entity.PriceLimitDkk = request.PriceLimitDkk.Value;           updated.Add("price_limit_dkk"); }
        if (request.AutoHeatingEnabled.HasValue) { entity.AutoHeatingEnabled = request.AutoHeatingEnabled.Value; updated.Add("auto_heating_enabled"); }
        if (request.AutoPumpEnabled.HasValue)    { entity.AutoPumpEnabled = request.AutoPumpEnabled.Value;       updated.Add("auto_pump_enabled"); }

        await _db.SaveChangesAsync();
        return (true, updated);
    }

    public async Task<ElectricityPrice?> GetElectricityPriceAsync(string date, string area)
    {
        var entity = await _db.ElectricityPrices
            .Include(e => e.Entries)
            .FirstOrDefaultAsync(e => e.Date == date && e.Area == area);

        return entity is null ? null : new ElectricityPrice
        {
            Date        = entity.Date,
            Area        = entity.Area,
            Currency    = entity.Currency,
            LastUpdated = entity.LastUpdated.ToString("o"),
            Prices      = entity.Entries
                .OrderBy(e => e.Hour)
                .Select(e => new PriceEntry
                {
                    Hour        = e.Hour.ToString("o"),
                    PriceDkkKwh = e.PriceDkkKwh
                })
                .ToList()
        };
    }

    private static DeviceSettings MapToDto(SettingsEntity e) => new()
    {
        MaxSandTemp         = e.MaxSandTemp,
        MinPumpTemp         = e.MinPumpTemp,
        PumpIntervalSeconds = e.PumpIntervalSeconds,
        PriceLimitDkk       = e.PriceLimitDkk,
        AutoHeatingEnabled  = e.AutoHeatingEnabled,
        AutoPumpEnabled     = e.AutoPumpEnabled
    };
}
