using System.Text.Json.Serialization;

namespace sandbattery_backend.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeasurementStatus
{
    [JsonStringEnumMemberName("OK")]      Ok,
    [JsonStringEnumMemberName("WARNING")] Warning,
    [JsonStringEnumMemberName("CRITICAL")]Critical,
    [JsonStringEnumMemberName("ERROR")]   Error
}
