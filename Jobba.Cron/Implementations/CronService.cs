using System;
using System.Linq;
using Cronos;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;

namespace Jobba.Cron.Implementations;

public class CronService : ICronService
{
    private static CronExpression GetCronExpression(string expression)
        => CronExpression.Parse(expression, CronFormat.IncludeSeconds);

    public DateTimeOffset? GetNextExecutionDate(string expression)
        => GetCronExpression(expression).GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc, true);

    public bool WillExecuteInWindow(string expression, DateTimeOffset start, DateTimeOffset end)
    {
        var next = GetNextExecutionDate(expression);

        if (next is null)
        {
            return false;
        }

        return next.Value.IsBetween(start, end);
    }

    public DateTimeOffset[] GetSchedule(string expression, DateTimeOffset start, DateTimeOffset end)
        => GetCronExpression(expression)
            .GetOccurrences(start, end, TimeZoneInfo.Utc)
            .ToArray();
}
