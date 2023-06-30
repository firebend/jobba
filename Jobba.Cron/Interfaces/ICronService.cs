using System;

namespace Jobba.Cron.Interfaces;

public interface ICronService
{
    DateTimeOffset? GetNextExecutionDate(string expression);

    bool WillExecuteInWindow(string expression, DateTimeOffset start, DateTimeOffset end);

    DateTimeOffset[] GetSchedule(string expression, DateTimeOffset start, DateTimeOffset end);
}
