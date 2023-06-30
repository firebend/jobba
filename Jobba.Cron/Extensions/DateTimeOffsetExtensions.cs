using System;

namespace Jobba.Cron.Extensions;

public static class DateTimeOffsetExtensions
{
    public static bool IsBetween(this DateTimeOffset input, DateTimeOffset min, DateTimeOffset max) => input >= min && input <= max;
}
