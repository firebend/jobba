using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

public interface IJobEventDispatcher
{
    public Task PublishStartedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishCompletedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishFaultedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishProgressAsync(Type jobType, Guid progressId, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishCancellationRequestAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishRestartAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, CancellationToken cancellationToken);
    public Task PublishWatchAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, TimeSpan delay, CancellationToken cancellationToken);
}
