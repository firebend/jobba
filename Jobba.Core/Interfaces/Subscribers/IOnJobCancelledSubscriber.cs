using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobCancelledSubscriber
    {
        Task OnJobCancelledAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
