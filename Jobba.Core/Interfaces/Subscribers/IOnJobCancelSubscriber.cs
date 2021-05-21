using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobCancelSubscriber
    {
        Task<bool> CancelJobAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken);
    }
}
