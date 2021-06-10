using System;

namespace Jobba.Core.Events
{
    public class JobRestartEvent
    {
        public Guid JobId { get; set; }

        public string JobParamsTypeName { get; set; }

        public string JobStateTypeName { get; set; }
    }
}
