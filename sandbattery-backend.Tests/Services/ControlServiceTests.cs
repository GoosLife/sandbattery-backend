using sandbattery_backend.Data.Entities;
using sandbattery_backend.Models;
using sandbattery_backend.Services;
using sandbattery_backend.Tests.Helpers;

namespace sandbattery_backend.Tests.Services;

public class ControlServiceTests
{
    private const string ProductKey = "TEST-0001";

    // ── Pump control ────────────────────────────────────────────────────────

    [Fact]
    public async Task ControlPump_Start_ReturnsSuccessAndLogsEvent()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var (success, result, tempExceeded) =
            await service.ControlPumpAsync(ProductKey, PumpAction.start, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);
        Assert.Equal("start", result.Action);
        Assert.Equal("manual", result.Source);
        Assert.True(result.EventId > 0);

        var ev = db.Events.Single(e => e.ProductKey == ProductKey);
        Assert.Equal("pump_start", ev.Type);
    }

    [Fact]
    public async Task ControlPump_Stop_SetsActuatorInactive()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        await service.ControlPumpAsync(ProductKey, PumpAction.start, CommandSource.manual);
        await service.ControlPumpAsync(ProductKey, PumpAction.stop, CommandSource.manual);

        var status = db.ActuatorStatuses.Single(a => a.ProductKey == ProductKey && a.Actuator == "pump");
        Assert.False(status.Active);
    }

    [Fact]
    public async Task ControlPump_CreatesExactlyOneActuatorRow()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        await service.ControlPumpAsync(ProductKey, PumpAction.start, CommandSource.manual);
        await service.ControlPumpAsync(ProductKey, PumpAction.stop, CommandSource.manual);
        await service.ControlPumpAsync(ProductKey, PumpAction.start, CommandSource.rule);

        var rows = db.ActuatorStatuses.Where(a => a.ProductKey == ProductKey && a.Actuator == "pump").ToList();
        Assert.Single(rows);
        Assert.Equal("rule", rows[0].Source);
    }

    // ── Heater control ──────────────────────────────────────────────────────

    [Fact]
    public async Task ControlHeater_On_WithTempBelowLimit_Succeeds()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Latest measurement is safely below the 70 °C limit
        db.SensorMeasurements.Add(new SensorMeasurementEntity
            { ProductKey = ProductKey, Timestamp = DateTime.UtcNow, SandTemp = 45f, Status = "OK" });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(ProductKey, HeaterAction.on, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);

        var status = db.ActuatorStatuses.Single(a => a.ProductKey == ProductKey && a.Actuator == "heater");
        Assert.True(status.Active);
    }

    [Fact]
    public async Task ControlHeater_On_WithTempAtLimit_ReturnsTempExceeded()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Latest measurement is exactly at the 70 °C limit
        db.SensorMeasurements.Add(new SensorMeasurementEntity
            { ProductKey = ProductKey, Timestamp = DateTime.UtcNow, SandTemp = 70f, Status = "CRITICAL" });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(ProductKey, HeaterAction.on, CommandSource.manual);

        Assert.False(success);
        Assert.True(tempExceeded);
        Assert.Null(result);

        // Heater state must NOT have changed
        Assert.Empty(db.ActuatorStatuses.Where(a => a.ProductKey == ProductKey && a.Actuator == "heater"));
    }

    [Fact]
    public async Task ControlHeater_Off_AlwaysSucceeds_EvenAboveLimit()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.Add(new SensorMeasurementEntity
            { ProductKey = ProductKey, Timestamp = DateTime.UtcNow, SandTemp = 85f, Status = "CRITICAL" });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(ProductKey, HeaterAction.off, CommandSource.rule);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ControlHeater_On_WithNoMeasurements_Succeeds()
    {
        // Without any measurements the safety check cannot trigger
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var (success, _, tempExceeded) =
            await service.ControlHeaterAsync(ProductKey, HeaterAction.on, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
    }

    // ── GetSystemStatus ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSystemStatus_NoActuatorRows_ReturnsDefaultsInactive()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var status = await service.GetSystemStatusAsync(ProductKey);

        Assert.False(status.Heater.Active);
        Assert.False(status.Pump.Active);
    }

    [Fact]
    public async Task GetSystemStatus_AfterCommands_ReflectsCurrentState()
    {
        await using var db = DbContextFactory.Create();
        await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.Add(new SensorMeasurementEntity
            { ProductKey = ProductKey, Timestamp = DateTime.UtcNow, SandTemp = 45f, Status = "OK" });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        await service.ControlPumpAsync(ProductKey, PumpAction.start, CommandSource.rule);
        await service.ControlHeaterAsync(ProductKey, HeaterAction.on, CommandSource.rule);

        var status = await service.GetSystemStatusAsync(ProductKey);

        Assert.True(status.Pump.Active);
        Assert.True(status.Heater.Active);
        Assert.Equal("rule", status.Pump.Source);
        Assert.Equal("rule", status.Heater.Source);
    }
}
