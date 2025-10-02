using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobFaultedSubscriber
{
    public Task OnJobFaultedAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken);
}
