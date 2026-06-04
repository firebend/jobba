using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using MassTransit;

namespace Jobba.MassTransit.Implementations;

public class MassTransitJobEventPublisher : IJobEventPublisher
{
    private readonly IBus _bus;
    private readonly IMessageScheduler _messageScheduler;
    private readonly ICancelRequestClientResolver _cancelRequestClientResolver;
    private readonly string _systemMoniker;

    public MassTransitJobEventPublisher(IBus bus,
        IMessageScheduler messageScheduler,
        ICancelRequestClientResolver cancelRequestClientResolver,
        IJobSystemInfoProvider systemInfoProvider)
    {
        _bus = bus;
        _messageScheduler = messageScheduler;
        _cancelRequestClientResolver = cancelRequestClientResolver;
        _systemMoniker = systemInfoProvider.GetSystemInfo()?.SystemMoniker ?? string.Empty;
    }

    public async Task PublishJobCancellationRequestAsync<TJob, TJobParams, TJobState>(CancelJobEvent<TJob> cancelJobEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        SetSystemMoniker(cancelJobEvent);
        _ = await _cancelRequestClientResolver.RequestCancellationAsync<TJob, TJobParams, TJobState>(
            cancelJobEvent,
            cancellationToken);
    }

    public Task PublishJobCancelledEventAsync<TJob>(JobCancelledEvent<TJob> jobCancelledEvent, CancellationToken cancellationToken)
        => PublishMessageAsync(jobCancelledEvent, null, cancellationToken);

    public Task PublishJobCompletedEventAsync<TJob>(JobCompletedEvent<TJob> jobCompletedEvent, CancellationToken cancellationToken)
        => PublishMessageAsync(jobCompletedEvent, null, cancellationToken);

    public Task PublishJobFaultedEventAsync<TJob>(JobFaultedEvent<TJob> jobFaultedEvent, CancellationToken cancellationToken)
        => PublishMessageAsync(jobFaultedEvent, null, cancellationToken);

    public Task PublishJobProgressEventAsync<TJob>(JobProgressEvent<TJob> jobProgressEvent, CancellationToken cancellationToken)
        => PublishMessageAsync(jobProgressEvent, null, cancellationToken);

    public Task PublishWatchJobEventAsync<TJob, TJobParams, TJobState>(JobWatchEvent<TJob> jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
        => PublishMessageAsync(jobWatchEvent, delay, cancellationToken);

    public Task PublishJobStartedEvent<TJob>(JobStartedEvent<TJob> jobStartedEvent, CancellationToken cancellationToken)
        => PublishMessageAsync(jobStartedEvent, null, cancellationToken);

    public Task PublishJobRestartEvent<TJob, TJobParams, TJobState>(JobRestartEvent<TJob> jobRestartEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
        => PublishMessageAsync(jobRestartEvent, null, cancellationToken);

    private void SetSystemMoniker(IJobbaEvent @event)
    {
        @event.SystemMoniker = _systemMoniker;
    }

    private Task PublishMessageAsync<T>(T message, TimeSpan? delay, CancellationToken cancellationToken) where T : class, IJobbaEvent
    {
        SetSystemMoniker(message);
        return delay.HasValue is false
            ? _bus.Publish(message, cancellationToken)
            : _messageScheduler.SchedulePublish(delay.Value, message, cancellationToken);
    }
}
