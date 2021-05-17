using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobFaultedSubscriber
    {
        Task OnJobFaultedAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
