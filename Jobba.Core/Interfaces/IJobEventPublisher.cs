using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for publishing job events.
/// </summary>
public interface IJobEventPublisher
{
    Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken);
    Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken);
    Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken);
    Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken);
    Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken);
    Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken);
    Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken);
    Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken);
}
