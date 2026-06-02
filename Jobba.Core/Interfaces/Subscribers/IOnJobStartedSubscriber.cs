using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobStartedSubscriber<TJob>
{
    public Task OnJobStartedAsync(JobStartedEvent<TJob> jobStartedEvent, CancellationToken cancellationToken);
}
