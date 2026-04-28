using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data.Entities;

namespace sandbattery_backend.Data;

public class SandbatteryDbContext : DbContext
{
    public SandbatteryDbContext(DbContextOptions<SandbatteryDbContext> options)
        : base(options) { }

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<SensorMeasurementEntity> SensorMeasurements => Set<SensorMeasurementEntity>();
    public DbSet<TemperatureSensorReadingEntity> TemperatureReadings => Set<TemperatureSensorReadingEntity>();
    public DbSet<FlowRateSensorReadingEntity> FlowRateReadings => Set<FlowRateSensorReadingEntity>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<ActuatorStatusEntity> ActuatorStatuses => Set<ActuatorStatusEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<ElectricityPriceEntity> ElectricityPrices => Set<ElectricityPriceEntity>();
    public DbSet<PriceEntryEntity> PriceEntries => Set<PriceEntryEntity>();
    public DbSet<HeartbeatEntity> Heartbeats => Set<HeartbeatEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceEntity>()
            .HasIndex(d => d.ProductKey)
            .IsUnique();

        modelBuilder.Entity<SensorMeasurementEntity>()
            .HasMany(m => m.TemperatureReadings)
            .WithOne()
            .HasForeignKey(r => r.MeasurementId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SensorMeasurementEntity>()
            .HasMany(m => m.FlowRateReadings)
            .WithOne()
            .HasForeignKey(r => r.MeasurementId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ElectricityPriceEntity>()
            .HasMany(e => e.Entries)
            .WithOne()
            .HasForeignKey(e => e.ElectricityPriceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActuatorStatusEntity>()
            .HasIndex(a => new { a.DeviceId, a.Actuator, a.ActuatorIndex })
            .IsUnique();

        modelBuilder.Entity<SettingsEntity>()
            .HasIndex(s => s.DeviceId)
            .IsUnique();

        modelBuilder.Entity<ElectricityPriceEntity>()
            .HasIndex(e => new { e.Date, e.Area })
            .IsUnique();
    }
}
