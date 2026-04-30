using sandbattery_backend.Models;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class SettingsServiceTests
{
    private const string ProductKey = "TEST-0001";

    [Fact]
    public async Task GetSettings_NoRow_ReturnsDefaults()
    {
        await using var db = DbContextFactory.Create();
        // Seed device but no settings row
        var device = new Data.Entities.DeviceEntity { ProductKey = ProductKey, DeviceName = "Test" };
        db.Devices.Add(device);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var settings = await service.GetSettingsAsync(device.Id);

        Assert.Equal(70.0f, settings.MaxSandTemp);
        Assert.Equal(50.0f, settings.MinPumpTemp);
        Assert.Equal(300, settings.PumpIntervalSeconds);
        Assert.Equal(1.50f, settings.PriceLimitDkk);
        Assert.True(settings.AutoHeatingEnabled);
        Assert.True(settings.AutoPumpEnabled);
    }

    [Fact]
    public async Task UpdateSettings_PartialUpdate_OnlyUpdatesSpecifiedFields()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var service = new SettingsService(db);
        var (success, updatedFields) = await service.UpdateSettingsAsync(deviceId, new SettingsUpdateRequest
        {
            PumpIntervalSeconds = 600,
            PriceLimitDkk = 2.50f
        });

        Assert.True(success);
        Assert.Equal(2, updatedFields.Count);
        Assert.Contains("pump_interval_seconds", updatedFields);
        Assert.Contains("price_limit_dkk", updatedFields);

        var settings = await service.GetSettingsAsync(deviceId);
        Assert.Equal(600, settings.PumpIntervalSeconds);
        Assert.Equal(2.50f, settings.PriceLimitDkk);
        // Untouched fields stay at defaults
        Assert.Equal(70.0f, settings.MaxSandTemp);
        Assert.Equal(50.0f, settings.MinPumpTemp);
    }

    [Fact]
    public async Task UpdateSettings_AllFields_UpdatesAll()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var service = new SettingsService(db);
        var (_, updatedFields) = await service.UpdateSettingsAsync(deviceId, new SettingsUpdateRequest
        {
            MaxSandTemp         = 65f,
            MinPumpTemp         = 45f,
            PumpIntervalSeconds = 120,
            PriceLimitDkk       = 1.0f,
            AutoHeatingEnabled  = false,
            AutoPumpEnabled     = false
        });

        Assert.Equal(6, updatedFields.Count);

        var settings = await service.GetSettingsAsync(deviceId);
        Assert.Equal(65f, settings.MaxSandTemp);
        Assert.Equal(45f, settings.MinPumpTemp);
        Assert.Equal(120, settings.PumpIntervalSeconds);
        Assert.Equal(1.0f, settings.PriceLimitDkk);
        Assert.False(settings.AutoHeatingEnabled);
        Assert.False(settings.AutoPumpEnabled);
    }

    [Fact]
    public async Task UpdateSettings_NoFields_ReturnsEmptyUpdatedList()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var service = new SettingsService(db);
        var (success, updatedFields) =
            await service.UpdateSettingsAsync(deviceId, new SettingsUpdateRequest());

        Assert.True(success);
        Assert.Empty(updatedFields);
    }

    [Fact]
    public async Task UpdateSettings_NoExistingRow_CreatesNewRow()
    {
        await using var db = DbContextFactory.Create();
        // Seed device but no settings row
        var device = new Data.Entities.DeviceEntity { ProductKey = ProductKey, DeviceName = "Test" };
        db.Devices.Add(device);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        await service.UpdateSettingsAsync(device.Id, new SettingsUpdateRequest { MaxSandTemp = 60f });

        var settings = await service.GetSettingsAsync(device.Id);
        Assert.Equal(60f, settings.MaxSandTemp);
    }
}
