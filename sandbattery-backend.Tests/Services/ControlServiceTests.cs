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
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var (success, result, tempExceeded) =
            await service.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);
        Assert.Equal("start", result.Action);
        Assert.Equal("manual", result.Source);
        Assert.True(result.EventId > 0);

        var ev = db.Events.Single(e => e.DeviceId == deviceId);
        Assert.Equal("pump_start", ev.Type);
    }

    [Fact]
    public async Task ControlPump_Stop_SetsActuatorInactive()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        await service.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.manual);
        await service.ControlPumpAsync(deviceId, PumpAction.stop, CommandSource.manual);

        var status = db.ActuatorStatuses.Single(a => a.DeviceId == deviceId && a.Actuator == "pump");
        Assert.False(status.Active);
    }

    [Fact]
    public async Task ControlPump_CreatesExactlyOneActuatorRow()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        await service.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.manual);
        await service.ControlPumpAsync(deviceId, PumpAction.stop, CommandSource.manual);
        await service.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.rule);

        var rows = db.ActuatorStatuses.Where(a => a.DeviceId == deviceId && a.Actuator == "pump").ToList();
        Assert.Single(rows);
        Assert.Equal("rule", rows[0].Source);
    }

    // ── Heater control ──────────────────────────────────────────────────────

    [Fact]
    public async Task ControlHeater_On_WithTempBelowLimit_Succeeds()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Latest measurement is safely below the 70 °C limit
        db.SensorMeasurements.Add(new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = DateTime.UtcNow,
            Status = "OK",
            TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 45f }]
        });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(deviceId, 0, HeaterAction.on, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);

        var status = db.ActuatorStatuses.Single(a => a.DeviceId == deviceId && a.Actuator == "heater");
        Assert.True(status.Active);
    }

    [Fact]
    public async Task ControlHeater_On_WithTempAtLimit_ReturnsTempExceeded()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        // Latest measurement is exactly at the 70 °C limit
        db.SensorMeasurements.Add(new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = DateTime.UtcNow,
            Status = "CRITICAL",
            TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 70f }]
        });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(deviceId, 0, HeaterAction.on, CommandSource.manual);

        Assert.False(success);
        Assert.True(tempExceeded);
        Assert.Null(result);

        // Heater state must NOT have changed
        Assert.Empty(db.ActuatorStatuses.Where(a => a.DeviceId == deviceId && a.Actuator == "heater"));
    }

    [Fact]
    public async Task ControlHeater_Off_AlwaysSucceeds_EvenAboveLimit()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.Add(new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = DateTime.UtcNow,
            Status = "CRITICAL",
            TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 85f }]
        });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        var (success, result, tempExceeded) =
            await service.ControlHeaterAsync(deviceId, 0, HeaterAction.off, CommandSource.rule);

        Assert.True(success);
        Assert.False(tempExceeded);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ControlHeater_On_WithNoMeasurements_Succeeds()
    {
        // Without any measurements the safety check cannot trigger
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var (success, _, tempExceeded) =
            await service.ControlHeaterAsync(deviceId, 0, HeaterAction.on, CommandSource.manual);

        Assert.True(success);
        Assert.False(tempExceeded);
    }

    // ── GetSystemStatus ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSystemStatus_NoActuatorRows_ReturnsDefaultsInactive()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);
        var service = new ControlService(db);

        var status = await service.GetSystemStatusAsync(deviceId);

        Assert.False(status.Heaters[0].Active);
        Assert.False(status.Pump.Active);
    }

    [Fact]
    public async Task GetSystemStatus_AfterCommands_ReflectsCurrentState()
    {
        await using var db = DbContextFactory.Create();
        var deviceId = await DbContextFactory.SeedDeviceAsync(db, ProductKey);

        db.SensorMeasurements.Add(new SensorMeasurementEntity
        {
            DeviceId = deviceId,
            Timestamp = DateTime.UtcNow,
            Status = "OK",
            TemperatureReadings = [new TemperatureSensorReadingEntity { SensorIndex = 0, Label = "sand", Value = 45f }]
        });
        await db.SaveChangesAsync();

        var service = new ControlService(db);
        await service.ControlPumpAsync(deviceId, PumpAction.start, CommandSource.rule);
        await service.ControlHeaterAsync(deviceId, 0, HeaterAction.on, CommandSource.rule);

        var status = await service.GetSystemStatusAsync(deviceId);

        Assert.True(status.Pump.Active);
        Assert.True(status.Heaters[0].Active);
        Assert.Equal("rule", status.Pump.Source);
        Assert.Equal("rule", status.Heaters[0].Source);
    }
}
