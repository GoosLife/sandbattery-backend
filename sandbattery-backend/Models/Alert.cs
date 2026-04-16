using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class Alert
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonIgnore]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; set; }
}

public class AlertList
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("alerts")]
    public List<Alert> Alerts { get; set; } = new();
}
