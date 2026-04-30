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
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db, new StubControlService());

        var result = await service.GetLatestMeasurementAsync(deviceId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestMeasurement_MultipleMeasurements_ReturnsMostRecent()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.AddRange(
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = DateTime.UtcNow.AddHours(-2), Status = "OK", TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 40f }] },
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = DateTime.UtcNow.AddHours(-1), Status = "OK", TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 50f }] },
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = DateTime.UtcNow,              Status = "OK", TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 60f }] }
        );
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var result = await service.GetLatestMeasurementAsync(deviceId);

        Assert.NotNull(result);
        Assert.Equal(60f, result.Temperatures.Single(t => t.Label == "sand").Value);
    }

    [Fact]
    public async Task GetLatestMeasurement_OnlyReturnsOwnDevice()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var otherId = await DbContextFactory.SeedDeviceAsync(db, "OTHER-DEVICE");

        db.SensorMeasurements.Add(new SensorMeasurementEntity
        {
            DeviceId = otherId,
            Timestamp = DateTime.UtcNow,
            Status = "OK",
            TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 99f }]
        });
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var result = await service.GetLatestMeasurementAsync(deviceId);

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
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Override default settings
        var settings = db.Settings.First(s => s.DeviceId == deviceId);
        settings.MaxSandTemp = maxSandTemp;
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var measurement = new SensorMeasurement
        {
            Timestamp  = DateTime.UtcNow.ToString("o"),
            Temperatures = [
                new TemperatureReading { Index = 0, Label = "sand",      Value = sandTemp },
                new TemperatureReading { Index = 1, Label = "water_in",  Value = 22f },
                new TemperatureReading { Index = 2, Label = "water_out", Value = 35f }
            ],
            FlowRates = [new FlowRateReading { Index = 0, Value = 3f }],
            PowerW = 1800f
        };

        var result = await service.AddMeasurementAsync(deviceId, measurement);

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
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db, new StubControlService());

        var result = await service.AddMeasurementAsync(deviceId, new SensorMeasurement
        {
            Timestamp  = DateTime.UtcNow.ToString("o"),
            Temperatures = [
                new TemperatureReading { Index = 0, Label = "sand",      Value = sandTemp },
                new TemperatureReading { Index = 1, Label = "water_in",  Value = waterTempIn },
                new TemperatureReading { Index = 2, Label = "water_out", Value = waterTempOut }
            ],
            FlowRates = [new FlowRateReading { Index = 0, Value = 3f }],
            PowerW = 1800f
        });

        Assert.Equal("ERROR", result.Status);
    }

    [Fact]
    public async Task AddMeasurement_CriticalTemp_CreatesAlert()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var service = new DataService(db, new StubControlService());
        await service.AddMeasurementAsync(deviceId, new SensorMeasurement
        {
            Timestamp  = DateTime.UtcNow.ToString("o"),
            Temperatures = [new TemperatureReading { Index = 0, Label = "sand", Value = 75f }],   // exceeds default max of 70
            FlowRates  = [new FlowRateReading { Index = 0, Value = 3f }],
            PowerW = 1800f
        });

        var alerts = db.Alerts.Where(a => a.DeviceId == deviceId).ToList();
        Assert.Single(alerts);
        Assert.Equal("CRITICAL", alerts[0].Severity);
        Assert.Equal("TEMP_LIMIT_EXCEEDED", alerts[0].Type);
        Assert.False(alerts[0].Acknowledged);
    }

    [Fact]
    public async Task AddMeasurement_OkTemp_DoesNotCreateAlert()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new DataService(db, new StubControlService());

        await service.AddMeasurementAsync(deviceId, new SensorMeasurement
        {
            Timestamp  = DateTime.UtcNow.ToString("o"),
            Temperatures = [new TemperatureReading { Index = 0, Label = "sand", Value = 45f }],
            FlowRates  = [new FlowRateReading { Index = 0, Value = 3f }],
            PowerW = 1800f
        });

        Assert.Empty(db.Alerts.Where(a => a.DeviceId == deviceId));
    }

    // ── GetMeasurementHistory ───────────────────────────────────────────────

    [Fact]
    public async Task GetMeasurementHistory_FiltersCorrectlyByTimeRange()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.SensorMeasurements.AddRange(
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = now.AddDays(-3), Status = "OK" },
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = now.AddDays(-1), Status = "OK" },
            new SensorMeasurementEntity { DeviceId = deviceId, Timestamp = now,              Status = "OK" }
        );
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var history = await service.GetMeasurementHistoryAsync(
            deviceId, now.AddDays(-2), now.AddHours(1), null, 1000);

        Assert.Equal(2, history.Count);
        Assert.Equal(2, history.Data.Count);
    }

    [Fact]
    public async Task GetMeasurementHistory_WithLimit_RespectsLimit()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        for (int i = 10; i >= 0; i--)
            db.SensorMeasurements.Add(new SensorMeasurementEntity
                { DeviceId = deviceId, Timestamp = now.AddMinutes(-i), Status = "OK" });
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var history = await service.GetMeasurementHistoryAsync(
            deviceId, now.AddHours(-1), now.AddMinutes(1), null, limit: 5);

        Assert.Equal(5, history.Count);
        Assert.Equal(5, history.Data.Count);
    }

    [Fact]
    public async Task GetMeasurementHistory_WithHourlyInterval_SamplesCorrectly()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var base_ = DateTime.UtcNow.AddHours(-5);
        // 6 measurements, one every 30 minutes → with 1h interval only 3 should be sampled
        for (int i = 0; i < 6; i++)
            db.SensorMeasurements.Add(new SensorMeasurementEntity
                { DeviceId = deviceId, Timestamp = base_.AddMinutes(i * 30), Status = "OK" });
        await db.SaveChangesAsync();

        var service = new DataService(db, new StubControlService());
        var history = await service.GetMeasurementHistoryAsync(
            deviceId, base_.AddMinutes(-1), base_.AddHours(3), MeasurementInterval.OneHour, 1000);

        Assert.Equal(3, history.Count);
    }
}
