using System;

namespace Jobba.Core.Models
{
    public class JobRequest<TJobParams, TJobState>
    {
        /// <summary>
        /// A description of what the job is doing.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The parameters to send to the job
        /// </summary>
        public TJobParams JobParameters { get; set; }

        /// <summary>
        /// The initial job state to set.
        /// </summary>
        public TJobState InitialJobState { get; set; }

        /// <summary>
        /// How often the job watcher needs to check in on this job to ensure it is still operating.
        /// </summary>
        public TimeSpan JobWatchInterval { get; set; }

        /// <summary>
        /// The type of the job to run
        /// </summary>
        public Type JobType { get; set; }

        /// <summary>
        /// True if the job is a restart; otherwise, false.
        /// </summary>
        public bool IsRestart { get; set; }

        /// <summary>
        /// The id of the job being restarted.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// The number of tries the job has be enqueued.
        /// </summary>
        public int NumberOfTries { get; set; } = 1;

        /// <summary>
        /// The maximum number of times the job can be tried.
        /// </summary>
        public int MaxNumberOfTries { get; set; } = 1;
    }
}
