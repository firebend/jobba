using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models;

/// <summary>
/// Information about a job's progress
/// </summary>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public record JobProgress<TJobState>
    where TJobState : IJobState
{
    /// <summary>
    ///     The job's id.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    ///     A progress update message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     The date the progress occurred.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    ///     A custom job state to save progress information with.
    /// </summary>
    public TJobState JobState { get; set; }

    /// <summary>
    ///     The percentage complete
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
