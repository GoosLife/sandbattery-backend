using System.Text.Json.Serialization;

namespace sandbattery_backend.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlertSeverity
{
    [JsonStringEnumMemberName("WARNING")] Warning,
    [JsonStringEnumMemberName("CRITICAL")]Critical,
    [JsonStringEnumMemberName("ERROR")]   Error
}
