using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class ActuatorStatus
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "manual";

    [JsonPropertyName("last_changed")]
    public string LastChanged { get; set; } = string.Empty;
}

public class SystemStatus
{
    [JsonPropertyName("heaters")]
    public List<ActuatorStatus> Heaters { get; set; } = [];

    [JsonPropertyName("pump")]
    public ActuatorStatus Pump { get; set; } = new();
}
