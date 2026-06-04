using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations;

public class JobTypeRegistry
{
    private readonly ConcurrentDictionary<Type, JobTypeDispatchEntry> _dispatchEntries = new();

    public void RegisterJobType<TJob, TJobParams, TJobState>()
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _dispatchEntries[typeof(TJob)] = new JobTypeDispatchEntry(
            Started: (pub, jobId, regId, ct) =>
                pub.PublishJobStartedEvent(new JobStartedEvent<TJob>(jobId, regId), ct),
            Completed: (pub, jobId, regId, ct) =>
                pub.PublishJobCompletedEventAsync(new JobCompletedEvent<TJob>(jobId, regId), ct),
            Faulted: (pub, jobId, regId, ct) =>
                pub.PublishJobFaultedEventAsync(new JobFaultedEvent<TJob>(jobId, regId), ct),
            Progress: (pub, progressId, jobId, regId, ct) =>
                pub.PublishJobProgressEventAsync(new JobProgressEvent<TJob>(progressId, jobId, regId), ct),
            CancellationRequest: (pub, jobId, regId, ct) =>
                pub.PublishJobCancellationRequestAsync<TJob, TJobParams, TJobState>(new CancelJobEvent<TJob>(jobId, regId), ct),
            Restart: (pub, jobId, paramsType, stateType, regId, ct) =>
                pub.PublishJobRestartEvent<TJob, TJobParams, TJobState>(new JobRestartEvent<TJob>(jobId, paramsType.AssemblyQualifiedName, stateType.AssemblyQualifiedName, regId), ct),
            Watch: (pub, jobId, paramsType, stateType, regId, delay, ct) =>
                pub.PublishWatchJobEventAsync<TJob, TJobParams, TJobState>(new JobWatchEvent<TJob>(jobId, paramsType.AssemblyQualifiedName, stateType.AssemblyQualifiedName, regId), delay, ct)
        );
    }

    public bool TryGetDispatchEntry(Type jobType, out JobTypeDispatchEntry entry)
        => _dispatchEntries.TryGetValue(jobType, out entry);
}

public record JobTypeDispatchEntry(
    Func<IJobEventPublisher, Guid, Guid, CancellationToken, Task> Started,
    Func<IJobEventPublisher, Guid, Guid, CancellationToken, Task> Completed,
    Func<IJobEventPublisher, Guid, Guid, CancellationToken, Task> Faulted,
    Func<IJobEventPublisher, Guid, Guid, Guid, CancellationToken, Task> Progress,
    Func<IJobEventPublisher, Guid, Guid, CancellationToken, Task> CancellationRequest,
    Func<IJobEventPublisher, Guid, Type, Type, Guid, CancellationToken, Task> Restart,
    Func<IJobEventPublisher, Guid, Type, Type, Guid, TimeSpan, CancellationToken, Task> Watch
);
