using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models;

/// <summary>
/// A request to invoked a registered job
/// </summary>
/// <typeparam name="TJobParams">
/// The type of job parameters.
/// </typeparam>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public record JobRequest<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    /// <summary>
    /// The id corresponding to the job registration.
    /// </summary>
    public Guid JobRegistrationId { get; set; }

    /// <summary>
    ///     A description of what the job is doing.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     The parameters to send to the job
    /// </summary>
    public TJobParams JobParameters { get; set; }

    /// <summary>
    ///     The initial job state to set.
    /// </summary>
    public TJobState InitialJobState { get; set; }

    /// <summary>
    ///     How often the job watcher needs to check in on this job to ensure it is still operating.
    /// </summary>
    public TimeSpan JobWatchInterval { get; set; }

    /// <summary>
    ///     The type of the job to run
    /// </summary>
    public Type JobType { get; set; }

    /// <summary>
    ///     True if the job is a restart; otherwise, false.
    /// </summary>
    public bool IsRestart { get; set; }

    /// <summary>
    ///     The id of the job being restarted.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    ///     The number of tries the job has be enqueued.
    /// </summary>
    public int NumberOfTries { get; set; } = 1;

    /// <summary>
    ///     The maximum number of times the job can be tried.
    /// </summary>
    public int MaxNumberOfTries { get; set; } = 1;

    public static JobRequest<TJobParams, TJobState> FromJobInfo(JobInfo<TJobParams, TJobState> info) => new()
    {
        Description = info.Description,
        IsRestart = true,
        JobId = info.Id,
        JobParameters = info.JobParameters,
        JobType = Type.GetType(info.JobType),
        InitialJobState = info.CurrentState,
        JobWatchInterval = info.JobWatchInterval,
        NumberOfTries = info.CurrentNumberOfTries + 1,
        MaxNumberOfTries = info.MaxNumberOfTries
    };
}
