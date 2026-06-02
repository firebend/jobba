using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

public interface IJobEventDispatcher
{
    Task PublishStartedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishCompletedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishFaultedAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishProgressAsync(Type jobType, Guid progressId, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishCancellationRequestAsync(Type jobType, Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishRestartAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, CancellationToken cancellationToken);
    Task PublishWatchAsync(Type jobType, Guid jobId, Type paramsType, Type stateType, Guid jobRegistrationId, TimeSpan delay, CancellationToken cancellationToken);
}
