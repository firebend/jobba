using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobCompletedSubscriber
    {
        Task OnJobCompletedAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
