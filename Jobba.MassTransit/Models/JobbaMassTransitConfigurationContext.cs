namespace Jobba.MassTransit.Models
{
    public class JobbaMassTransitConfigurationContext
    {
        public string QueuePrefix { get; set; } = "Jobba";
        public JobbaMassTransitQueueMode QueueMode { get; set; } = JobbaMassTransitQueueMode.OneQueue;
        public string ReceiveEndpointPrefix { get; set; } = string.Empty;
    }
}
