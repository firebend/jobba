using System;

namespace Jobba.Cron.Interfaces;

public interface ICronService
{
    /// <summary>
    /// Gets the next time the expression should execute. Assumes DateTimeOffset.UtcNow for start window
    /// </summary>
    /// <param name="expression">
    /// The cron expression
    /// </param>
    /// <returns>
    /// The date
    /// </returns>
    DateTimeOffset? GetNextExecutionDate(string expression);

    /// <summary>
    /// Gets the next time the expression should execute given a specific start date
    /// </summary>
    /// <param name="expression">
    /// The cron expression
    /// </param>
    /// <param name="start">
    /// The <see cref="DateTimeOffset"/> to assume the start window for
    /// </param>
    /// <returns>
    /// The date
    /// </returns>
    DateTimeOffset? GetNextExecutionDate(string expression, DateTimeOffset start);

    /// <summary>
    /// Gets a value indicating if the job will occur in the given <see cref="DateTimeOffset"/> window
    /// </summary>
    /// The cron expression
    /// </param>
    /// <param name="start">
    /// The <see cref="DateTimeOffset"/> to assume the start window for
    /// </param>
    /// <param name="end">
    /// The <see cref="DateTimeOffset"/> to assume the start window for
    /// </param>
    /// <returns>
    /// True if the cron will execute; otherwise, false.
    /// </returns>
    bool WillExecuteInWindow(string expression, DateTimeOffset start, DateTimeOffset end);

    /// <summary>
    /// Gets a value indicating all the of occurrences for a cron expression in the given <see cref="DateTimeOffset"/> window.
    /// </summary>
    /// The cron expression
    /// </param>
    /// <param name="start">
    /// The <see cref="DateTimeOffset"/> to assume the start window for
    /// </param>
    /// <param name="end">
    /// The <see cref="DateTimeOffset"/> to assume the start window for
    /// </param>
    /// <returns>
    /// An array of all the execution dates.
    /// </returns>
    DateTimeOffset[] GetSchedule(string expression, DateTimeOffset start, DateTimeOffset end);
}
