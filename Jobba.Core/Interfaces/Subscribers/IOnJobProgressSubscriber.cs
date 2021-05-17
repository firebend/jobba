using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobProgressSubscriber
    {
        Task OnJobProgressAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken);
    }
}
