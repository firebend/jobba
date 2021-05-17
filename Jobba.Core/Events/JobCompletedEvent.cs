using System;

namespace Jobba.Core.Events
{
    public class JobCompletedEvent
    {
        /// <summary>
        /// The id of the job that was completed.
        /// </summary>
        public Guid JobId { get; set; }
    }
}
