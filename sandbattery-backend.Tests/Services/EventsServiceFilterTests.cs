using sandbattery_backend.Data.Entities;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class EventsServiceFilterTests
{
    private const string ProductKey = "TEST-0001";

    private static EventEntity MakeEvent(string type, string source, DateTime timestamp) => new()
    {
        ProductKey = ProductKey, Type = type, Source = source,
        Timestamp = timestamp, Description = ""
    };

    // ── Source filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_FilterBySource_ReturnsMatchingOnly()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            MakeEvent("pump_start", "manual", now.AddMinutes(-3)),
            MakeEvent("heat_on",    "rule",   now.AddMinutes(-2)),
            MakeEvent("pump_stop",  "rule",   now.AddMinutes(-1))
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(ProductKey, null, null, null, "rule", 100, 0);

        Assert.Equal(2, result.Total);
        Assert.All(result.Events, e => Assert.Equal("rule", e.Source));
    }

    [Fact]
    public async Task GetEvents_FilterBySource_NoMatch_ReturnsEmpty()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.Events.Add(MakeEvent("pump_start", "manual", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(ProductKey, null, null, null, "rule", 100, 0);

        Assert.Equal(0, result.Total);
    }

    // ── Date range filter ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_FilterByDateRange_ExcludesOutsideRange()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var now = DateTime.UtcNow;
        db.Events.AddRange(
            MakeEvent("pump_start", "manual", now.AddDays(-5)),  // too old
            MakeEvent("heat_on",    "rule",   now.AddDays(-2)),  // inside
            MakeEvent("pump_stop",  "manual", now.AddDays(-1))   // inside
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(
            ProductKey, now.AddDays(-3), now, null, null, 100, 0);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetEvents_FilterByTo_ExcludesNewerEvents()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var cutoff = DateTime.UtcNow.AddHours(-1);
        db.Events.AddRange(
            MakeEvent("pump_start", "manual", cutoff.AddMinutes(-10)), // before cutoff
            MakeEvent("heat_on",    "rule",   cutoff.AddMinutes(10))   // after cutoff
        );
        await db.SaveChangesAsync();

        var service = new EventsService(db);
        var result = await service.GetEventsAsync(
            ProductKey, null, cutoff, null, null, 100, 0);

        Assert.Equal(1, result.Total);
        Assert.Equal("pump_start", result.Events[0].Type);
    }
}
