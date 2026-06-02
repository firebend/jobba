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
    public Task PublishJobCancellationRequestAsync<TJob, TJobParams, TJobState>(CancelJobEvent<TJob> cancelJobEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
    public Task PublishJobCancelledEventAsync<TJob>(JobCancelledEvent<TJob> jobCancelledEvent, CancellationToken cancellationToken);
    public Task PublishJobCompletedEventAsync<TJob>(JobCompletedEvent<TJob> jobCompletedEvent, CancellationToken cancellationToken);
    public Task PublishJobFaultedEventAsync<TJob>(JobFaultedEvent<TJob> jobFaultedEvent, CancellationToken cancellationToken);
    public Task PublishJobProgressEventAsync<TJob>(JobProgressEvent<TJob> jobProgressEvent, CancellationToken cancellationToken);
    public Task PublishWatchJobEventAsync<TJob, TJobParams, TJobState>(JobWatchEvent<TJob> jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
    public Task PublishJobStartedEvent<TJob>(JobStartedEvent<TJob> jobStartedEvent, CancellationToken cancellationToken);
    public Task PublishJobRestartEvent<TJob, TJobParams, TJobState>(JobRestartEvent<TJob> jobRestartEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
}
