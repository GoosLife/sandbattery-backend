using sandbattery_backend.Data.Entities;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class SettingsServiceElectricityPriceTests
{
    private const string ProductKey = "TEST-0001";

    [Fact]
    public async Task GetElectricityPrice_MatchingRow_ReturnsPriceWithEntries()
    {
        await using var db = DbContextFactory.Create();
        _ = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        var ep = new ElectricityPriceEntity
        {
            Date = "2025-06-01", Area = "DK2", Currency = "DKK",
            LastUpdated = new DateTime(2025, 6, 1, 11, 0, 0, DateTimeKind.Utc)
        };
        db.ElectricityPrices.Add(ep);
        await db.SaveChangesAsync();

        db.PriceEntries.AddRange(
            new PriceEntryEntity { ElectricityPriceId = ep.Id, Hour = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), PriceDkkKwh = 0.82f },
            new PriceEntryEntity { ElectricityPriceId = ep.Id, Hour = new DateTime(2025, 6, 1, 1, 0, 0, DateTimeKind.Utc), PriceDkkKwh = 0.74f }
        );
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.GetElectricityPriceAsync("2025-06-01", "DK2");

        Assert.NotNull(result);
        Assert.Equal("2025-06-01", result.Date);
        Assert.Equal("DK2", result.Area);
        Assert.Equal("DKK", result.Currency);
        Assert.Equal(2, result.Prices.Count);
        Assert.Equal(0.82f, result.Prices[0].PriceDkkKwh);
    }

    [Fact]
    public async Task GetElectricityPrice_NoMatchingRow_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        _ = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new SettingsService(db);

        var result = await service.GetElectricityPriceAsync("2025-06-01", "DK2");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetElectricityPrice_WrongArea_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.ElectricityPrices.Add(new ElectricityPriceEntity
            { Date = "2025-06-01", Area = "DK1", Currency = "DKK", LastUpdated = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.GetElectricityPriceAsync("2025-06-01", "DK2");

        Assert.Null(result);
    }
}
