using System;

namespace Jobba.Core.Models
{
    public class JobProgress<TJobState>
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
        public TJobState JobState { get; set; }

        /// <summary>
        /// The percentage complete
        /// </summary>
        public decimal Progress { get; set; }
    }
}
