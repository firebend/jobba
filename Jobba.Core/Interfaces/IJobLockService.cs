using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    //todo: impl
    public interface IJobLockService
    {
        Task<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
