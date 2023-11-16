using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models.Entities;

public class JobEntity : IJobbaEntity
{
    /// <summary>
    ///     The type of job that was enqueued
    /// </summary>
    public string JobType { get; set; }

    /// <summary>
    ///     The job's description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The job's name
    /// </summary>
    public string JobName { get; set; }

    /// <summary>
    ///     The last progress logged for the job.
    /// </summary>
    public decimal LastProgressPercentage { get; set; }

    /// <summary>
    ///     The last date progress was logged for the job.
    /// </summary>
    public DateTimeOffset LastProgressDate { get; set; }

    /// <summary>
    ///     The Job's status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    ///     The time the job was started
    /// </summary>
    public DateTimeOffset EnqueuedTime { get; set; }

    /// <summary>
    ///     If the job has faulted the reason it did.
    /// </summary>
    public string FaultedReason { get; set; }

    /// <summary>
    ///     The maximum number of times the job can be retried.
    /// </summary>
    public int MaxNumberOfTries { get; set; }

    /// <summary>
    ///     How many times the job has already been tried.
    /// </summary>
    public int CurrentNumberOfTries { get; set; }

    public TimeSpan JobWatchInterval { get; set; }

    public IJobParams JobParameters { get; set; }

    public IJobState JobState { get; set; }

    public string JobStateTypeName { get; set; }

    public string JobParamsTypeName { get; set; }

    public bool IsOutOfRetry { get; set; }

    /// <summary>
    ///     The job's id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }

    /// <summary>
    /// Information about the system that the job is running on.
    /// </summary>
    public JobSystemInfo SystemInfo { get; set; }

    public static JobEntity FromRequest<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest,
        Guid jobRegistrationId,
        JobSystemInfo jobSystemInfo)
        where TJobParams : IJobParams
        where TJobState : IJobState => new()
        {
            Description = jobRequest.Description,
            Id = jobRequest.JobId,
            Status = JobStatus.Unknown,
            EnqueuedTime = DateTimeOffset.UtcNow,
            FaultedReason = null,
            JobType = jobRequest.JobType.AssemblyQualifiedName,
            JobWatchInterval = jobRequest.JobWatchInterval,
            LastProgressDate = DateTimeOffset.UtcNow,
            LastProgressPercentage = 0,
            CurrentNumberOfTries = jobRequest.NumberOfTries,
            MaxNumberOfTries = jobRequest.MaxNumberOfTries,
            JobParameters = jobRequest.JobParameters,
            JobState = jobRequest.InitialJobState,
            JobParamsTypeName = typeof(TJobParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TJobState).AssemblyQualifiedName,
            IsOutOfRetry = jobRequest.MaxNumberOfTries <= jobRequest.NumberOfTries,
            JobRegistrationId = jobRegistrationId,
            JobName = jobRequest.JobName,
            SystemInfo = jobSystemInfo
        };

    public JobInfo<TJobParams, TJobState> ToJobInfo<TJobParams, TJobState>()
        where TJobParams : IJobParams
        where TJobState : IJobState => new()
        {
            Description = Description,
            Id = Id,
            Status = Status,
            CurrentState = (TJobState)JobState,
            EnqueuedTime = EnqueuedTime,
            FaultedReason = FaultedReason,
            JobParameters = (TJobParams)JobParameters,
            JobType = JobType,
            JobWatchInterval = JobWatchInterval,
            LastProgressDate = LastProgressDate,
            LastProgressPercentage = LastProgressPercentage,
            CurrentNumberOfTries = CurrentNumberOfTries,
            MaxNumberOfTries = MaxNumberOfTries,
            JobParamsTypeName = JobParamsTypeName,
            JobStateTypeName = JobStateTypeName,
            IsOutOfRetry = MaxNumberOfTries <= CurrentNumberOfTries,
            JobRegistrationId = JobRegistrationId,
            JobName = JobName,
            SystemInfo = SystemInfo
        };

    public JobInfoBase ToJobInfoBase() => new()
    {
        Description = Description,
        Id = Id,
        Status = Status,
        EnqueuedTime = EnqueuedTime,
        FaultedReason = FaultedReason,
        JobType = JobType,
        JobWatchInterval = JobWatchInterval,
        LastProgressDate = LastProgressDate,
        LastProgressPercentage = LastProgressPercentage,
        CurrentNumberOfTries = CurrentNumberOfTries,
        MaxNumberOfTries = MaxNumberOfTries,
        IsOutOfRetry = MaxNumberOfTries <= CurrentNumberOfTries,
        JobParamsTypeName = JobParamsTypeName,
        JobStateTypeName = JobStateTypeName,
        JobRegistrationId = JobRegistrationId,
        JobName = JobName,
        SystemInfo = SystemInfo
    };
}
