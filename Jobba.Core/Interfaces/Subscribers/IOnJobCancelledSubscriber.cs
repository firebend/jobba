using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobCancelledSubscriber
{
    public Task OnJobCancelledAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken);
}
