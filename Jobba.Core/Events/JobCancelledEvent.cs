using System;

namespace Jobba.Core.Events
{
    public class JobCancelledEvent
    {
        /// <summary>
        /// The id of the job that was cancelled.
        /// </summary>
        public Guid JobId { get; set; }
    }
}
