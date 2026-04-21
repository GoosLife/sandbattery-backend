using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidateProductKey_WithValidKey_ReturnsDevice()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, "ABC-1234", "Sandbatteri – Ringsted Kommune");
        var service = new AuthService(db);

        var result = await service.ValidateProductKeyAsync("ABC-1234");

        Assert.NotNull(result);
        Assert.Equal("ABC-1234", result.ProductKey);
        Assert.Equal("Sandbatteri – Ringsted Kommune", result.DeviceName);
    }

    [Fact]
    public async Task ValidateProductKey_WithUnknownKey_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, "ABC-1234");
        var service = new AuthService(db);

        var result = await service.ValidateProductKeyAsync("WRONG-KEY");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateProductKey_EmptyDatabase_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        var service = new AuthService(db);

        var result = await service.ValidateProductKeyAsync("ABC-1234");

        Assert.Null(result);
    }
}
