using System;

namespace Jobba.MassTransit.Models;

public class JobbaMassTransitConfigurationContext
{
    /// <summary>
    ///     A prefix for all the queues. Defaults to "Jobba"
    /// </summary>
    public string QueuePrefix { get; set; } = "Jobba";

    /// <summary>
    ///     The queue mode to operate in. Defaults to OneQueue
    /// </summary>
    public JobbaMassTransitQueueMode QueueMode { get; set; } = JobbaMassTransitQueueMode.OneQueue;

    /// <summary>
    ///     A prefix for each receiving endpoint. you can use this if you are sharing a bus and want to reduce collisions.
    /// </summary>
    public string ReceiveEndpointPrefix { get; set; } = string.Empty;

    /// <summary>
    ///     How often to try and request a job be cancelled. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan CancelJobRequestInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The max number of times we should try to cancel a job. Defaults to 10.
    /// </summary>
    public int MaxTimesToRequestJobCancellation { get; set; } = 10;
}
