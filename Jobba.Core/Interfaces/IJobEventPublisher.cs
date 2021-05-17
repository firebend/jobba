using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    public interface IJobEventPublisher
    {
        Task PublishJobCancellationRequestAsync(Guid jobId, CancellationToken cancellationToken);
        Task PublishJobCancelledEventAsync(Guid jobId, CancellationToken cancellationToken);
        Task PublishJobCompletedEventAsync(Guid jobId, CancellationToken cancellationToken);
        Task PublishJobFaultedEventAsync(Guid jobId, CancellationToken cancellationToken);
        Task PublishJobProgressEventAsync(Guid jobProgressId, CancellationToken cancellationToken);
        Task PublishWatchJobEventAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
