using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobStartedSubscriber
    {
        Task OnJobStartedAsync(JobStartedEvent jobCompletedEvent, CancellationToken cancellationToken);
    }
}
