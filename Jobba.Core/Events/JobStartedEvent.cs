using System;
using Jobba.Core.Models;

namespace Jobba.Core.Events
{
    public class JobStartedEvent
    {
        public Guid JobId { get; set; }

        public JobStartedEvent()
        {

        }

        public JobStartedEvent(Guid jobId)
        {
            JobId = jobId;
        }
    }
}
