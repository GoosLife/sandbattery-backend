using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data.Entities;

namespace sandbattery_backend.Data;

public class SandbatteryDbContext : DbContext
{
    public SandbatteryDbContext(DbContextOptions<SandbatteryDbContext> options)
        : base(options) { }

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<SensorMeasurementEntity> SensorMeasurements => Set<SensorMeasurementEntity>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<ActuatorStatusEntity> ActuatorStatuses => Set<ActuatorStatusEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<ElectricityPriceEntity> ElectricityPrices => Set<ElectricityPriceEntity>();
    public DbSet<PriceEntryEntity> PriceEntries => Set<PriceEntryEntity>();
    public DbSet<HeartbeatEntity> Heartbeats => Set<HeartbeatEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ElectricityPriceEntity>()
            .HasMany(e => e.Entries)
            .WithOne()
            .HasForeignKey(e => e.ElectricityPriceId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per (product_key, actuator)
        modelBuilder.Entity<ActuatorStatusEntity>()
            .HasIndex(a => new { a.ProductKey, a.Actuator })
            .IsUnique();

        // One settings row per device
        modelBuilder.Entity<SettingsEntity>()
            .HasIndex(s => s.ProductKey)
            .IsUnique();

        // One electricity price per (date, area)
        modelBuilder.Entity<ElectricityPriceEntity>()
            .HasIndex(e => new { e.Date, e.Area })
            .IsUnique();
    }
}
