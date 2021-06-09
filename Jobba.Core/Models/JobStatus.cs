namespace Jobba.Core.Models
{
    public enum JobStatus
    {
        /// <summary>
        /// The job state is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The job faulted because of an error.
        /// </summary>
        Faulted = 1,

        /// <summary>
        /// The job ran to completion.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// The job is running.
        /// </summary>
        InProgress = 3,

        /// <summary>
        /// The job was requested to be started but hasn't yet started running.
        /// </summary>
        Enqueued = 4,

        /// <summary>
        /// An actor requested the job be cancelled.
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// The job was cancelled because the app running the job scheduler shut down.
        /// </summary>
        ForceCancelled = 6,
    }
}
