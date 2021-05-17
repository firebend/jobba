using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces
{
    public interface IJobEventPublisher
    {
        Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken);
        Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken);
        Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken);
        Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken);
        Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken);
        Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, CancellationToken cancellationToken);
        Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken);
    }
}
