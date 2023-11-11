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

    /// <summary>
    /// The default number of tries to use for the job.
    /// </summary>
    public int DefaultMaxNumberOfTries { get; set; } = 5;

    /// <summary>
    /// The default job watch interval to use for the job.
    /// </summary>
    public TimeSpan DefaultJobWatchInterval { get; set; } = TimeSpan.FromSeconds(10);

    public DateTimeOffset? PreviousExecutionDate { get; set; }

    public DateTimeOffset? NextExecutionDate { get; set; }

    public string Description { get; set; }

    public static JobRegistration FromTypes<TJob, TJobParams, TJobState>(string name,
        string description = null,
        string cron = null)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState => new()
    {
        JobName = name,
        JobType = typeof(TJob),
        JobParamsType = typeof(TJobParams),
        JobStateType = typeof(TJobState),
        CronExpression = cron,
        Description = description,
    };
}
