using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("heartbeat")]
public class HeartbeatEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("uptime_seconds")]
    public int? UptimeSeconds { get; set; }
}
