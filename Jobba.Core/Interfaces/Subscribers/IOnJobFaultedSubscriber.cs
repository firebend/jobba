using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobFaultedSubscriber<TJob>
{
    public Task OnJobFaultedAsync(JobFaultedEvent<TJob> jobFaultedEvent, CancellationToken cancellationToken);
}
