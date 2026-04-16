using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class Event
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonIgnore]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class EventList
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("events")]
    public List<Event> Events { get; set; } = new();
}
