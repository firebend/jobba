using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    public interface IJobLockService
    {
        Task<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
