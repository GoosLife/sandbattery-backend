using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class ActuatorStatus
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "manual";

    [JsonPropertyName("last_changed")]
    public string LastChanged { get; set; } = string.Empty;
}

public class SystemStatus
{
    [JsonPropertyName("heater")]
    public ActuatorStatus Heater { get; set; } = new();

    [JsonPropertyName("pump")]
    public ActuatorStatus Pump { get; set; } = new();
}
