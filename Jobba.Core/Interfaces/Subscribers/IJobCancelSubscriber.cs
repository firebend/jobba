using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IJobCancelSubscriber
    {
        Task CancelJob(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken);
    }
}
