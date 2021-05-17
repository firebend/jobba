using System;

namespace Jobba.Core.Events
{
    public class JobFaultedEvent
    {
        /// <summary>
        /// The id of the job that has faulted.
        /// </summary>
        public Guid JobId { get; set; }
    }
}
