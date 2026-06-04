using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class JobEventDispatcher : IJobEventDispatcher
{
    private readonly ILogger<JobEventDispatcher> _logger;
    private readonly IJobEventPublisher _publisher;
    private readonly JobTypeRegistry _registry;

    public JobEventDispatcher(ILogger<JobEventDispatcher> logger, IJobEventPublisher publisher, JobTypeRegistry registry)
    {
        _logger = logger;
        _publisher = publisher;
        _registry = registry;
    }

    private JobTypeDispatchEntry GetEntry(Type jobType)
    {
        if (_registry.TryGetDispatchEntry(jobType, out var entry))
        {
            return entry;
        }

        _logger.LogWarning("No job type dispatch entry registered for {JobType}", jobType);
        return null;
    }

    public Task PublishStartedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Started(_publisher, jobId, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishCompletedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Completed(_publisher, jobId, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishFaultedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Faulted(_publisher, jobId, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishProgressAsync(Type jobType, Guid progressId, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Progress(_publisher, progressId, jobId, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishCancellationRequestAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.CancellationRequest(_publisher, jobId, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishRestartAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Restart(_publisher, jobId, paramsType, stateType, jobRegistrationId, cancellationToken) ?? Task.CompletedTask;

    public Task PublishWatchAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, TimeSpan delay, CancellationToken cancellationToken)
        => GetEntry(jobType)?.Watch(_publisher, jobId, paramsType, stateType, jobRegistrationId, delay, cancellationToken) ?? Task.CompletedTask;
}
