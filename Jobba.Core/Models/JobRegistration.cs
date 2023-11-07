using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models;

/// <summary>
/// Encapsulates information about registering a job with the scheduler.
/// </summary>
public class JobRegistration : IJobbaEntity
{
    /// <summary>
    /// The Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The Job's Name
    /// </summary>
    public string JobName { get; set; }

    /// <summary>
    /// The type of job to register
    /// </summary>
    public Type JobType { get; set; }

    /// <summary>
    /// The type of job params that will be passed to the job
    /// </summary>
    public Type JobParamsType { get; set; }

    /// <summary>
    /// The type of job state that will be passed to the job
    /// </summary>
    public Type JobStateType { get; set; }

    /// <summary>
    /// The cron expression to use for scheduling the job.
    /// </summary>
    public string CronExpression { get; set; }
}
