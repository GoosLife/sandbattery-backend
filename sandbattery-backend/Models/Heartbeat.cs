using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class Heartbeat
{
    [JsonPropertyName("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("uptime_seconds")]
    public int? UptimeSeconds { get; set; }
}
