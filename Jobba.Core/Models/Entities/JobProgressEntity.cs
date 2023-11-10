using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Models.Entities;

public class JobProgressEntity : IJobbaEntity
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
    public IJobState JobState { get; set; }

    /// <summary>
    ///     The percentage complete
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }

    public Guid Id { get; set; }

    public static JobProgressEntity FromJobProgress<TJobState>(JobProgress<TJobState> progress)
        where TJobState : IJobState => new()
    {
        Date = progress.Date,
        Message = progress.Message,
        Progress = progress.Progress,
        JobId = progress.JobId,
        JobState = progress.JobState,
        JobRegistrationId = progress.JobRegistrationId
    };
}
