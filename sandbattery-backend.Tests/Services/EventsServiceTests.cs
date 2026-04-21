using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class EventsServiceTests
{
    private const string ProductKey = "TEST-0001";

    // ── GetEvents ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_NoEvents_ReturnsEmptyList()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new EventsService(db);

        var result = await service.GetEventsAsync(ProductKey, null, null, null, null, 100, 0);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task GetEvents_ReturnsMostRecentFirst()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            new EventEntity { ProductKey = ProductKey, Type = "pump_start", Source = "manual", Timestamp = now.AddHours(-2), Description = "A" },
            new EventEntity { ProductKey = ProductKey, Type = "pump_stop",  Source = "manual", Timestamp = now.AddHours(-1), Description = "B" },
            new EventEntity { ProductKey = ProductKey, Type = "heat_on",    Source = "rule",   Timestamp = now,              Description = "C" }
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(ProductKey, null, null, null, null, 100, 0);

        Assert.Equal(3, result.Total);
        Assert.Equal("heat_on", result.Events[0].Type);
        Assert.Equal("pump_start", result.Events[2].Type);
    }

    [Fact]
    public async Task GetEvents_FilterByType_ReturnsMatchingOnly()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            new EventEntity { ProductKey = ProductKey, Type = "pump_start", Source = "manual", Timestamp = now.AddMinutes(-3), Description = "" },
            new EventEntity { ProductKey = ProductKey, Type = "heat_on",    Source = "rule",   Timestamp = now.AddMinutes(-2), Description = "" },
            new EventEntity { ProductKey = ProductKey, Type = "pump_stop",  Source = "manual", Timestamp = now.AddMinutes(-1), Description = "" }
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(
            ProductKey, null, null, new[] { "pump_start", "pump_stop" }, null, 100, 0);

        Assert.Equal(2, result.Total);
        Assert.All(result.Events, e => Assert.StartsWith("pump", e.Type));
    }

    [Fact]
    public async Task GetEvents_Pagination_SkipsAndLimitsCorrectly()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
            db.Events.Add(new EventEntity
                { ProductKey = ProductKey, Type = "pump_start", Source = "manual", Timestamp = now.AddMinutes(-i), Description = $"Event {i}" });
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var page1 = await service.GetEventsAsync(ProductKey, null, null, null, null, 3, 0);
        var page2 = await service.GetEventsAsync(ProductKey, null, null, null, null, 3, 3);

        Assert.Equal(10, page1.Total);
        Assert.Equal(3, page1.Events.Count);
        Assert.Equal(3, page2.Events.Count);
        Assert.NotEqual(page1.Events[0].Timestamp, page2.Events[0].Timestamp);
    }

    // ── Alerts ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveAlerts_OnlyReturnsUnacknowledged()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.Alerts.AddRange(
            new AlertEntity { ProductKey = ProductKey, Severity = "CRITICAL", Type = "TEMP_LIMIT_EXCEEDED", Message = "Hot", Timestamp = DateTime.UtcNow.AddMinutes(-5), Acknowledged = false },
            new AlertEntity { ProductKey = ProductKey, Severity = "WARNING",  Type = "SENSOR_OFFLINE",      Message = "Cold", Timestamp = DateTime.UtcNow.AddMinutes(-3), Acknowledged = true }
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetActiveAlertsAsync(ProductKey);

        Assert.Equal(1, result.Count);
        Assert.Equal("TEMP_LIMIT_EXCEEDED", result.Alerts[0].Type);
    }

    [Fact]
    public async Task AcknowledgeAlert_ValidId_MarksAcknowledged()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.Alerts.Add(new AlertEntity
            { ProductKey = ProductKey, Severity = "WARNING", Type = "SENSOR_OFFLINE", Message = "Test", Timestamp = DateTime.UtcNow, Acknowledged = false });
        await db.SaveChangesAsync();

        var alertId = db.Alerts.First().Id;
        var service = new EventsService(db);

        var result = await service.AcknowledgeAlertAsync(alertId, ProductKey);

        Assert.NotNull(result);
        Assert.True(result.Acknowledged);

        // Verify persisted
        var entity = db.Alerts.Find(alertId)!;
        Assert.True(entity.Acknowledged);
    }

    [Fact]
    public async Task AcknowledgeAlert_WrongProductKey_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.Alerts.Add(new AlertEntity
            { ProductKey = ProductKey, Severity = "WARNING", Type = "TEST", Message = "Test", Timestamp = DateTime.UtcNow, Acknowledged = false });
        await db.SaveChangesAsync();

        var alertId = db.Alerts.First().Id;
        var service = new EventsService(db);

        var result = await service.AcknowledgeAlertAsync(alertId, "WRONG-KEY");

        Assert.Null(result);
    }

    [Fact]
    public async Task AcknowledgeAlert_NonExistentId_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new EventsService(db);

        var result = await service.AcknowledgeAlertAsync(9999, ProductKey);

        Assert.Null(result);
    }

    // ── Heartbeat ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddHeartbeat_PersistsToDatabase()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new EventsService(db);

        await service.AddHeartbeatAsync(new Heartbeat
        {
            ProductKey    = ProductKey,
            Timestamp     = DateTime.UtcNow.ToString("o"),
            UptimeSeconds = 86400
        });

        var entity = db.Heartbeats.Single();
        Assert.Equal(ProductKey, entity.ProductKey);
        Assert.Equal(86400, entity.UptimeSeconds);
    }
}
