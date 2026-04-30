using sandbattery_backend.Data.Entities;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class EventsServiceFilterTests
{
    private const string ProductKey = "TEST-0001";

    private static EventEntity MakeEvent(int deviceId, string type, string source, DateTime timestamp) => new()
    {
        DeviceId = deviceId, Type = type, Source = source,
        Timestamp = timestamp, Description = ""
    };

    // ── Source filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_FilterBySource_ReturnsMatchingOnly()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            MakeEvent(deviceId, "pump_start", "manual", now.AddMinutes(-3)),
            MakeEvent(deviceId, "heat_on",    "rule",   now.AddMinutes(-2)),
            MakeEvent(deviceId, "pump_stop",  "rule",   now.AddMinutes(-1))
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(deviceId, null, null, null, "rule", 100, 0);

        Assert.Equal(2, result.Total);
        Assert.All(result.Events, e => Assert.Equal("rule", e.Source));
    }

    [Fact]
    public async Task GetEvents_FilterBySource_NoMatch_ReturnsEmpty()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.Events.Add(MakeEvent(deviceId, "pump_start", "manual", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(deviceId, null, null, null, "rule", 100, 0);

        Assert.Equal(0, result.Total);
    }

    // ── Date range filter ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_FilterByDateRange_ExcludesOutsideRange()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            MakeEvent(deviceId, "pump_start", "manual", now.AddDays(-5)),  // too old
            MakeEvent(deviceId, "heat_on",    "rule",   now.AddDays(-2)),  // inside
            MakeEvent(deviceId, "pump_stop",  "manual", now.AddDays(-1))   // inside
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(
            deviceId, now.AddDays(-3), now, null, null, 100, 0);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetEvents_FilterByTo_ExcludesNewerEvents()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var cutoff = DateTime.UtcNow.AddHours(-1);
        db.Events.AddRange(
            MakeEvent(deviceId, "pump_start", "manual", cutoff.AddMinutes(-10)), // before cutoff
            MakeEvent(deviceId, "heat_on",    "rule",   cutoff.AddMinutes(10))   // after cutoff
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(
            deviceId, null, cutoff, null, null, 100, 0);

        Assert.Equal(1, result.Total);
        Assert.Equal("pump_start", result.Events[0].Type);
    }
}
