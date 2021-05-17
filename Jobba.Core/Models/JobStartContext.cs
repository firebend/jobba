using System;

namespace Jobba.Core.Models
{
    public class JobStartContext<TJob, TJobParams, TJobState>
    {
        /// <summary>
        /// The parameters to start the job with.
        /// </summary>
        public TJobParams JobParameters {get; set; }

        /// <summary>
        /// The state to start the job with.
        /// </summary>
        public TJobState JobState {get; set; }

        /// <summary>
        /// The time the job was started.
        /// </summary>
        public DateTimeOffset StartTime {get;set;}

        /// <summary>
        /// The id pointing to the worker that started the job.
        /// </summary>
        public string WorkerId {get;set;}

        /// <summary>
        /// The job's id.
        /// </summary>
        public Guid JobId {get;set;}
    }
}
