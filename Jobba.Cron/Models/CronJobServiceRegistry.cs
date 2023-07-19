using System;

namespace Jobba.Cron.Models;

/// <summary>
/// Encapsulates metadata about a cron job.
/// This class is used for tracking jobs registered in the applications service collection.
/// </summary>
public class CronJobServiceRegistry
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
}
