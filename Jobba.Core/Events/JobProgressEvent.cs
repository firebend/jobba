using System;

namespace Jobba.Core.Events
{
    public class JobProgressEvent
    {
        /// <summary>
        /// An id pointing to the progress entity with progress information.
        /// </summary>
        public Guid JobProgressId { get; set; }

        public JobProgressEvent()
        {

        }

        public JobProgressEvent(Guid jobProgressId)
        {
            JobProgressId = jobProgressId;
        }
    }
}
