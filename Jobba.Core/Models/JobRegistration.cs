using System;

namespace Jobba.Core.Models;

/// <summary>
/// Encapsulates information about registering a job with the scheduler.
/// </summary>
public record JobRegistration
{
    /// <summary>
    /// The Id
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of job to register
    /// </summary>
    public Type JobType { get; set; }

    /// <summary>
    /// The type of job params that will be passed to the job
    /// </summary>
    public Type JobParamsType { get; init; }

    /// <summary>
    /// The type of job state that will be passed to the job
    /// </summary>
    public Type JobStateType { get; init; }
}

public record CronJobRegistration : JobRegistration
{
    /// <summary>
    /// The cron expression to use for scheduling the job.
    /// </summary>
    public string CronExpression { get; init; }
}
