using System;

namespace Jobba.Core.Models;

/// <summary>
/// The context for starting a job.
/// </summary>
/// <typeparam name="TJobParams">
/// The type of job parameters.
/// </typeparam>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public record JobStartContext<TJobParams, TJobState>
{
    /// <summary>
    ///     The parameters to start the job with.
    /// </summary>
    public TJobParams JobParameters { get; set; }

    /// <summary>
    ///     The state to start the job with.
    /// </summary>
    public TJobState JobState { get; set; }

    /// <summary>
    ///     The time the job was started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    ///     The job's id.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public JobRegistration JobRegistration { get; set; }

    /// <summary>
    ///     The current number of times the job has been tried.
    /// </summary>
    public int CurrentNumberOfTries { get; set; }

    /// <summary>
    ///     True if the job has been restarted; otherwise false signifying the first attempt.
    /// </summary>
    public bool IsRestart { get; set; }

    /// <summary>
    ///     If the job is restarted, the last percentage complete that was logged.
    /// </summary>
    public decimal? LastProgressPercentage { get; set; }

    /// <summary>
    ///     If the job is restarted, the last date progress complete that was logged.
    /// </summary>
    public DateTimeOffset? LastProgressDate { get; set; }
}
