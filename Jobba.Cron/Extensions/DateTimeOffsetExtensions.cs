using System;

namespace Jobba.Cron.Extensions;

public static class DateTimeOffsetExtensions
{
    public static bool IsBetween(this DateTimeOffset input, DateTimeOffset min, DateTimeOffset max) => input >= min && input <= max;

    public static DateTimeOffset TrimMilliseconds(this DateTimeOffset source) => new (
        source.Year,
        source.Month,
        source.Day,
        source.Hour,
        source.Minute,
        source.Second,
        0,
        source.Offset);
}
