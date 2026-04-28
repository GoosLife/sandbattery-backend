using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("actuator_status")]
public class ActuatorStatusEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("actuator")]
    public string Actuator { get; set; } = string.Empty;

    [Column("actuator_index")]
    public int ActuatorIndex { get; set; } = 0;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; }

    [Column("source")]
    public string Source { get; set; } = "manual";

    [Column("last_changed")]
    public DateTime LastChanged { get; set; }
}
