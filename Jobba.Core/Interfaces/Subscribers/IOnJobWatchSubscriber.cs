using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobWatchSubscriber
{
    Task WatchJobAsync(JobWatchEvent jobWatchEvent, CancellationToken cancellationToken);
}
