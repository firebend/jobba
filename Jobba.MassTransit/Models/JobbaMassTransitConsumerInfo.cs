using System;

namespace Jobba.MassTransit.Models
{
    public class JobbaMassTransitConsumerInfo
    {
        public Type ConsumerType { get; set; }

        public string EntityActionDescription { get; set; }
    }
}
