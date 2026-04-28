using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("settings")]
public class SettingsEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("max_sand_temp")]
    public float MaxSandTemp { get; set; } = 70.0f;

    [Column("min_pump_temp")]
    public float MinPumpTemp { get; set; } = 50.0f;

    [Column("pump_interval_seconds")]
    public int PumpIntervalSeconds { get; set; } = 300;

    [Column("price_limit_dkk")]
    public float PriceLimitDkk { get; set; } = 1.50f;

    [Column("auto_heating_enabled")]
    public bool AutoHeatingEnabled { get; set; } = true;

    [Column("auto_pump_enabled")]
    public bool AutoPumpEnabled { get; set; } = true;
}
