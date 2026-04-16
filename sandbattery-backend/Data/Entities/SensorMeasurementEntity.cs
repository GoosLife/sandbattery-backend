using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("sensor_measurement")]
public class SensorMeasurementEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("sand_temp")]
    public float SandTemp { get; set; }

    [Column("water_temp_in")]
    public float WaterTempIn { get; set; }

    [Column("water_temp_out")]
    public float WaterTempOut { get; set; }

    [Column("flow_rate")]
    public float FlowRate { get; set; }

    [Column("power_w")]
    public float PowerW { get; set; }

    [Column("energy_kwh")]
    public float EnergyKwh { get; set; }

    [Column("status")]
    public string Status { get; set; } = "OK";
}
