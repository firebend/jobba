using System;

namespace Jobba.Core.Models.Entities
{
    public class JobProgressEntity
    {
        /// <summary>
        /// The job's id.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// A progress update message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The date the progress occurred.
        /// </summary>
        public DateTimeOffset Date { get; set; }

        /// <summary>
        /// A custom job state to save progress information with.
        /// </summary>
        public object JobState { get; set; }

        /// <summary>
        /// The percentage complete
        /// </summary>
        public decimal Progress { get; set; }

        public static JobProgressEntity FromJobProgress<TJobState>(JobProgress<TJobState> progress) => new()
        {
            Date = progress.Date,
            Message = progress.Message,
            Progress = progress.Progress,
            JobId = progress.JobId,
            JobState = progress.JobState
        };
    }
}
