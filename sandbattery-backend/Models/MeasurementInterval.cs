namespace sandbattery_backend.Models;

public enum MeasurementInterval
{
    OneMinute,
    FiveMinutes,
    FifteenMinutes,
    ThirtyMinutes,
    OneHour,
    SixHours,
    OneDay
}

public static class MeasurementIntervalExtensions
{
    public static MeasurementInterval? TryParse(string? value) => value switch
    {
        "1m" => MeasurementInterval.OneMinute,
        "5m" => MeasurementInterval.FiveMinutes,
        "15m" => MeasurementInterval.FifteenMinutes,
        "30m" => MeasurementInterval.ThirtyMinutes,
        "1h" => MeasurementInterval.OneHour,
        "6h" => MeasurementInterval.SixHours,
        "1d" => MeasurementInterval.OneDay,
        _ => null
    };

    public static string ToApiString(this MeasurementInterval interval) => interval switch
    {
        MeasurementInterval.OneMinute => "1m",
        MeasurementInterval.FiveMinutes => "5m",
        MeasurementInterval.FifteenMinutes => "15m",
        MeasurementInterval.ThirtyMinutes => "30m",
        MeasurementInterval.OneHour => "1h",
        MeasurementInterval.SixHours => "6h",
        MeasurementInterval.OneDay => "1d",
        _ => throw new ArgumentOutOfRangeException(nameof(interval))
    };

    public static TimeSpan ToTimeSpan(this MeasurementInterval interval) => interval switch
    {
        MeasurementInterval.OneMinute => TimeSpan.FromMinutes(1),
        MeasurementInterval.FiveMinutes => TimeSpan.FromMinutes(5),
        MeasurementInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
        MeasurementInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
        MeasurementInterval.OneHour => TimeSpan.FromHours(1),
        MeasurementInterval.SixHours => TimeSpan.FromHours(6),
        MeasurementInterval.OneDay => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(interval))
    };
}
