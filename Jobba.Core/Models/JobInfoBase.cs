using System;

namespace Jobba.Core.Models;

/// <summary>
/// Base information about a job.
/// </summary>
public record JobInfoBase
{
    /// <summary>
    ///     The job's id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }

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
    public string JobStateTypeName { get; set; }
    public string JobParamsTypeName { get; set; }
    public bool IsOutOfRetry { get; set; }
}
