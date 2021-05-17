using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobProgressSubscriber
    {
        Task OnJobProgressAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
