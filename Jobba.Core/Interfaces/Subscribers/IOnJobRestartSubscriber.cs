using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobRestartSubscriber
{
    Task OnJobRestartAsync(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken);
}
