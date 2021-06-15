using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    public interface IJobReScheduler
    {
        Task RestartFaultedJobsAsync(CancellationToken cancellationToken);
    }
}
