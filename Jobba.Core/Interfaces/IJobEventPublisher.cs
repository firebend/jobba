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
    public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken);
    public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken);
    public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken);
    public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken);
    public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken);
    public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken);
    public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken);
    public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken);
}
