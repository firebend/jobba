using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models;

/// <summary>
/// Encapsulates information about registering a job with the scheduler.
/// </summary>
public class JobRegistration : IJobbaEntity
{
    private string _timeZoneId = "UTC";
    private TimeZoneInfo _timeZoneInfo;

    /// <summary>
    /// The Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The Job's Name
    /// </summary>
    public required string JobName { get; set; }

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

    /// <summary>
    /// The last time the job executed
    /// </summary>
    public DateTimeOffset? PreviousExecutionDate { get; set; }

    /// <summary>
    /// The next time the job will execute
    /// </summary>
    public DateTimeOffset? NextExecutionDate { get; set; }

    /// <summary>
    /// The job's description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The job's default state
    /// </summary>
    public IJobState DefaultState { get; set; }

    /// <summary>
    /// The job's default parameters
    /// </summary>
    public IJobParams DefaultParams { get; set; }

    /// <summary>
    /// True if the registration should be inactive, thus preventing future jobs from being invoked; otherwise, true.
    /// </summary>
    public bool IsInactive { get; set; }

    /// <summary>
    /// The system moniker the job is associated with.
    /// </summary>
    public required string SystemMoniker { get; set; }

    /// <summary>
    /// The time zone id to use for the job. Must be resolvable by <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>.
    /// </summary>
    public string TimeZoneId
    {
        get => _timeZoneId;
        set
        {
            _timeZoneId = value;
            _timeZoneInfo = null;
        }
    }

    public TimeZoneInfo TimeZoneInfo => _timeZoneInfo ??= TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId ?? "UTC");

    public static JobRegistration FromTypes<TJob, TJobParams, TJobState>(
        string systemMoniker,
        string name,
        string description = default,
        string cron = default,
        TJobParams defaultJobParams = default,
        TJobState defaultJobState = default,
        bool isInactive = false,
        string timeZone = "UTC")
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState => new()
        {
            SystemMoniker = systemMoniker,
            JobName = name,
            JobType = typeof(TJob),
            JobParamsType = typeof(TJobParams),
            JobStateType = typeof(TJobState),
            CronExpression = cron,
            Description = description,
            DefaultParams = defaultJobParams,
            DefaultState = defaultJobState,
            IsInactive = isInactive,
            TimeZoneId = timeZone
        };
}
