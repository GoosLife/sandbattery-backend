using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PumpAction { start, stop }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HeaterAction { on, off }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandSource { manual, rule }

public class PumpCommandRequest
{
    [JsonPropertyName("action")]
    public PumpAction Action { get; set; }

    [JsonPropertyName("source")]
    public CommandSource Source { get; set; }
}

public class HeaterCommandRequest
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("action")]
    public HeaterAction Action { get; set; }

    [JsonPropertyName("source")]
    public CommandSource Source { get; set; }
}
