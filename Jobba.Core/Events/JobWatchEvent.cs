using System;

namespace Jobba.Core.Events
{
    public class JobWatchEvent
    {
        /// <summary>
        /// The id of the job that needs to be watched.
        /// </summary>
        public Guid JobId { get; set; }

        public JobWatchEvent()
        {

        }

        public JobWatchEvent(Guid jobId)
        {
            JobId = jobId;
        }
    }
}
