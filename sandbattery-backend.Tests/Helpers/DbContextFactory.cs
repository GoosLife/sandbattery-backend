using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;

namespace sandbattery_backend.Tests.Helpers;

public static class DbContextFactory
{
    /// <summary>
    /// Creates a fresh in-memory DbContext with a unique database name per call
    /// so tests are fully isolated from each other.
    /// </summary>
    public static SandbatteryDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<SandbatteryDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new SandbatteryDbContext(options);
    }

    /// <summary>
    /// Seeds a test device and its default settings into the context.
    /// Returns the generated device ID.
    /// </summary>
    public static async Task<int> SeedDeviceAsync(
        SandbatteryDbContext db,
        string productKey = "TEST-0001",
        string deviceName = "Test Sandbatteri")
    {
        var device = new DeviceEntity { ProductKey = productKey, DeviceName = deviceName };
        db.Devices.Add(device);
        await db.SaveChangesAsync();

        db.Settings.Add(new SettingsEntity { DeviceId = device.Id });
        await db.SaveChangesAsync();

        return device.Id;
    }
}
