using System;

namespace Jobba.Core.Events
{
    public class JobProgressEvent
    {
        /// <summary>
        /// An id pointing to the progress entity with progress information.
        /// </summary>
        public Guid JobProgressId { get; set; }

        /// <summary>
        /// The Id of the job reporting progress
        /// </summary>
        public Guid JobId { get; set; }

        public JobProgressEvent()
        {

        }

        public JobProgressEvent(Guid jobProgressId, Guid jobId)
        {
            JobProgressId = jobProgressId;
            JobId = jobId;
        }
    }
}
