namespace Jobba.Core.Models;

/// <summary>
/// Information about a job, its parameters, and its state.
/// </summary>
/// <typeparam name="TJobParams">
/// The type of job parameters.
/// </typeparam>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public record JobInfo<TJobParams, TJobState> : JobInfoBase
{
    /// <summary>
    ///     The parameters that were passed to the job.
    /// </summary>
    public TJobParams JobParameters { get; set; }

    /// <summary>
    ///     The current state of the job.
    /// </summary>
    public TJobState CurrentState { get; set; }
}
