using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobProgressSubscriber<TJob>
{
    public Task OnJobProgressAsync(JobProgressEvent<TJob> jobProgressEvent, CancellationToken cancellationToken);
}
