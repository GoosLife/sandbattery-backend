using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class DataServiceTests
{
    private const string ProductKey = "TEST-0001";

    // ── GetLatestMeasurement ────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestMeasurement_NoData_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db);

        var result = await service.GetLatestMeasurementAsync(ProductKey);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestMeasurement_MultipleMeasurements_ReturnsMostRecent()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.AddRange(
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = DateTime.UtcNow.AddHours(-2), SandTemp = 40f, Status = "OK" },
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = DateTime.UtcNow.AddHours(-1), SandTemp = 50f, Status = "OK" },
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = DateTime.UtcNow,              SandTemp = 60f, Status = "OK" }
        );
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var result = await service.GetLatestMeasurementAsync(ProductKey);

        Assert.NotNull(result);
        Assert.Equal(60f, result.SandTemp);
    }

    [Fact]
    public async Task GetLatestMeasurement_OnlyReturnsOwnDevice()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.Add(new SensorMeasurementEntity
            { ProductKey = "OTHER-DEVICE", Timestamp = DateTime.UtcNow, SandTemp = 99f, Status = "OK" });
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var result = await service.GetLatestMeasurementAsync(ProductKey);

        Assert.Null(result);
    }

    // ── Status determination ────────────────────────────────────────────────

    [Theory]
    [InlineData(45f, 70f, "OK")]
    [InlineData(63f, 70f, "WARNING")]   // 63 >= 70 * 0.9 = 63
    [InlineData(70f, 70f, "CRITICAL")]
    [InlineData(75f, 70f, "CRITICAL")]
    public async Task AddMeasurement_AssignsCorrectStatus(float sandTemp, float maxSandTemp, string expectedStatus)
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Override default settings
        var settings = db.Settings.First(s => s.ProductKey == ProductKey);
        settings.MaxSandTemp = maxSandTemp;
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var measurement = new SensorMeasurement
        {
            Timestamp    = DateTime.UtcNow.ToString("o"),
            ProductKey   = ProductKey,
            SandTemp     = sandTemp,
            WaterTempIn  = 22f,
            WaterTempOut = 35f,
            FlowRate     = 3f,
            PowerW       = 1800f,
            EnergyKwh    = 1f
        };

        var result = await service.AddMeasurementAsync(ProductKey, measurement);

        Assert.Equal(expectedStatus, result.Status);
    }

    [Theory]
    [InlineData(-127f, 22f, 35f)]   // Sand sensor disconnected
    [InlineData(45f, -127f, 35f)]   // Water-in sensor disconnected
    [InlineData(45f, 22f, -127f)]   // Water-out sensor disconnected
    public async Task AddMeasurement_DisconnectedSensor_AssignsErrorStatus(
        float sandTemp, float waterTempIn, float waterTempOut)
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db);

        var result = await service.AddMeasurementAsync(ProductKey, new SensorMeasurement
        {
            Timestamp    = DateTime.UtcNow.ToString("o"),
            ProductKey   = ProductKey,
            SandTemp     = sandTemp,
            WaterTempIn  = waterTempIn,
            WaterTempOut = waterTempOut,
            FlowRate     = 3f,
            PowerW       = 1800f,
            EnergyKwh    = 1f
        });

        Assert.Equal("ERROR", result.Status);
    }

    [Fact]
    public async Task AddMeasurement_CriticalTemp_CreatesAlert()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var service = new DataService(db);
        await service.AddMeasurementAsync(ProductKey, new SensorMeasurement
        {
            Timestamp    = DateTime.UtcNow.ToString("o"),
            ProductKey   = ProductKey,
            SandTemp     = 75f,   // exceeds default max of 70
            WaterTempIn  = 22f,
            WaterTempOut = 35f,
            FlowRate     = 3f,
            PowerW       = 1800f,
            EnergyKwh    = 1f
        });

        var alerts = db.Alerts.Where(a => a.ProductKey == ProductKey).ToList();
        Assert.Single(alerts);
        Assert.Equal("CRITICAL", alerts[0].Severity);
        Assert.Equal("TEMP_LIMIT_EXCEEDED", alerts[0].Type);
        Assert.False(alerts[0].Acknowledged);
    }

    [Fact]
    public async Task AddMeasurement_OkTemp_DoesNotCreateAlert()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db);

        await service.AddMeasurementAsync(ProductKey, new SensorMeasurement
        {
            Timestamp    = DateTime.UtcNow.ToString("o"),
            ProductKey   = ProductKey,
            SandTemp     = 45f,
            WaterTempIn  = 22f,
            WaterTempOut = 35f,
            FlowRate     = 3f,
            PowerW       = 1800f,
            EnergyKwh    = 1f
        });

        Assert.Empty(db.Alerts.Where(a => a.ProductKey == ProductKey));
    }

    // ── GetMeasurementHistory ───────────────────────────────────────────────

    [Fact]
    public async Task GetMeasurementHistory_FiltersCorrectlyByTimeRange()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.SensorMeasurements.AddRange(
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = now.AddDays(-3), SandTemp = 40f, Status = "OK" },
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = now.AddDays(-1), SandTemp = 50f, Status = "OK" },
            new SensorMeasurementEntity { ProductKey = ProductKey, Timestamp = now,              SandTemp = 60f, Status = "OK" }
        );
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var history = await service.GetMeasurementHistoryAsync(
            ProductKey, now.AddDays(-2), now.AddHours(1), null, 1000);

        Assert.Equal(2, history.Count);
        Assert.Equal(2, history.Data.Count);
    }

    [Fact]
    public async Task GetMeasurementHistory_WithLimit_RespectsLimit()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        for (int i = 10; i >= 0; i--)
            db.SensorMeasurements.Add(new SensorMeasurementEntity
                { ProductKey = ProductKey, Timestamp = now.AddMinutes(-i), SandTemp = 40f, Status = "OK" });
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var history = await service.GetMeasurementHistoryAsync(
            ProductKey, now.AddHours(-1), now.AddMinutes(1), null, limit: 5);

        Assert.Equal(5, history.Count);
        Assert.Equal(5, history.Data.Count);
    }

    [Fact]
    public async Task GetMeasurementHistory_WithHourlyInterval_SamplesCorrectly()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var base_ = DateTime.UtcNow.AddHours(-5);
        // 6 measurements, one every 30 minutes → with 1h interval only 3 should be sampled
        for (int i = 0; i < 6; i++)
            db.SensorMeasurements.Add(new SensorMeasurementEntity
                { ProductKey = ProductKey, Timestamp = base_.AddMinutes(i * 30), SandTemp = 40f, Status = "OK" });
        await db.SaveChangesAsync();

        var service = new DataService(db);
        var history = await service.GetMeasurementHistoryAsync(
            ProductKey, base_.AddMinutes(-1), base_.AddHours(3), MeasurementInterval.OneHour, 1000);

        Assert.Equal(3, history.Count);
    }
}
