using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobCompletedSubscriber
{
    Task OnJobCompletedAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken);
}
