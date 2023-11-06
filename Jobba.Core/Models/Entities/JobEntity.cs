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

    public object JobParameters { get; set; }

    public object JobState { get; set; }

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

    public static JobEntity FromRequest<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest) => new()
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
        JobParamsTypeName = jobRequest.JobParameters.GetType().AssemblyQualifiedName,
        JobStateTypeName = jobRequest.InitialJobState.GetType().AssemblyQualifiedName,
        IsOutOfRetry = jobRequest.MaxNumberOfTries <= jobRequest.NumberOfTries,
        JobRegistrationId = jobRequest.JobRegistrationId
    };

    public JobInfo<TJobParams, TJobState> ToJobInfo<TJobParams, TJobState>() => new()
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
        JobRegistrationId = JobRegistrationId
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
        JobRegistrationId = JobRegistrationId
    };
}
