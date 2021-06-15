using System;

namespace Jobba.MassTransit.Models
{
    public class JobbaMassTransitConsumerInfo
    {
        public Type ConsumerType { get; set; }

        public string QueueName { get; set; }
    }
}
