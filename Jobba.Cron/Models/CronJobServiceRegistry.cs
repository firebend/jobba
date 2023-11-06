using System;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;

namespace Jobba.Cron.Models;

/// <summary>
/// Encapsulates metadata about a cron job.
/// This class is used for tracking jobs registered in the applications service collection.
/// </summary>
public record CronJobServiceRegistry
{
    /// <summary>
    /// The cron expression
    /// </summary>
    public string Cron { get; set; }

    /// <summary>
    /// The type that implements <see cref="ICronJob"/>
    /// </summary>
    public Type JobType { get; set; }

    /// <summary>
    /// The job's name
    /// </summary>
    public string JobName { get; set; }

    /// <summary>
    /// The type of parameters the job will use
    /// </summary>
    public Type JobParamsType { get; set; }

    /// <summary>
    /// The type of state the job will use
    /// </summary>
    public Type JobStateType { get; set; }

    /// <summary>
    /// The job's description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// How often jobba should check in on the job to make sure its completed
    /// </summary>
    public TimeSpan WatchInterval { get; set; } = TimeSpan.FromSeconds(30);

    public DateTimeOffset? PreviousExecutionDate { get; set; }

    public DateTimeOffset? NextExecutionDate { get; set; }

    /// <summary>
    /// True if the job should have fired in the window; otherwise, false.
    /// </summary>
    /// <param name="min">
    /// The min time frame
    /// </param>
    /// <param name="max">
    /// The max time frame
    /// </param>
    /// <returns>
    /// True if the job should execute; otherwise, false.
    /// </returns>
    public bool ShouldExecute(DateTimeOffset min, DateTimeOffset max)
    {
        var isInWindow = NextExecutionDate >= min && NextExecutionDate <= max;

        //********************************************
        // Author: JMA
        // Date: 2023-07-20 03:36:12
        // Comment: Job has not previously executed, we should invoke it
        //*******************************************
        if (PreviousExecutionDate is null)
        {
            return isInWindow;
        }

        //********************************************
        // Author: JMA
        // Date: 2023-07-20 03:39:37
        // Comment: If we are in the window of execution, make sure our previous execution is old enough
        // this will prevent the hosted service from triggering jobs too frequently
        //*******************************************
        var should = PreviousExecutionDate <= min && isInWindow;

        return should;
    }

    /// <summary>
    /// Sets the next execution time for the job.
    /// </summary>
    /// <param name="service">
    /// A <see cref="ICronService"/> to calculate the next execution date
    /// </param>
    /// <param name="start">
    /// An optional window to pass to check for occurrences. If null now is used.
    /// </param>
    public void SetNextExecutionDate(ICronService service, DateTimeOffset? start = null)
    {
        start ??= DateTimeOffset.UtcNow;

        var next = service.GetNextExecutionDate(Cron, start.Value);

        if (next is not null)
        {
            NextExecutionDate = next.Value.TrimSeconds();
        }
    }
}
