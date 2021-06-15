namespace Jobba.Core.Models
{
    public class JobInfo<TJobParams, TJobState> : JobInfoBase
    {

        /// <summary>
        /// The parameters that were passed to the job.
        /// </summary>
        public TJobParams JobParameters { get; set; }

        /// <summary>
        /// The current state of the job.
        /// </summary>
        public TJobState CurrentState { get; set; }
    }
}
