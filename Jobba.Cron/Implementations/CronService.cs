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

    public DateTimeOffset? GetNextExecutionDate(string expression)
        => GetNextExecutionDate(expression, DateTimeOffset.UtcNow);

    public DateTimeOffset? GetNextExecutionDate(string expression, DateTimeOffset start)
        => GetCronExpression(expression).GetNextOccurrence(start.ToUniversalTime(), TimeZoneInfo.Utc, true);

    public bool WillExecuteInWindow(string expression, DateTimeOffset start, DateTimeOffset end)
    {
        var next = GetNextExecutionDate(expression);

        return next is not null && next.Value.IsBetween(start, end);
    }

    public DateTimeOffset[] GetSchedule(string expression, DateTimeOffset start, DateTimeOffset end)
        => GetCronExpression(expression)
            .GetOccurrences(start, end, TimeZoneInfo.Utc)
            .ToArray();
}
