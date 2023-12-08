using System;
using System.Linq;
using Cronos;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;

namespace Jobba.Cron.Implementations;

public class CronService : ICronService
{
    private static CronExpression GetCronExpression(string expression)
        => CronExpression.Parse(expression, CronFormat.Standard);

    public DateTimeOffset? GetNextExecutionDate(string expression, TimeZoneInfo timeZone)
        => GetNextExecutionDate(expression, DateTimeOffset.UtcNow, timeZone);

    public DateTimeOffset? GetNextExecutionDate(string expression, DateTimeOffset start, TimeZoneInfo timeZone)
        => GetCronExpression(expression).GetNextOccurrence(start, timeZone, true);

    public bool WillExecuteInWindow(string expression, DateTimeOffset start, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        var next = GetNextExecutionDate(expression, timeZone);

        return next is not null && next.Value.IsBetween(start, end);
    }

    public DateTimeOffset[] GetSchedule(string expression, DateTimeOffset start, DateTimeOffset end, TimeZoneInfo timeZone)
        => GetCronExpression(expression)
            .GetOccurrences(start, end, timeZone)
            .ToArray();
}
