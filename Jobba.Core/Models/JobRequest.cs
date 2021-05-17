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
    }
}
