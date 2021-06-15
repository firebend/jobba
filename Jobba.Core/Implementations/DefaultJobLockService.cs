using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Concurrency;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations
{
    public class DefaultJobLockService : IJobLockService
    {
        public Task<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            var locker = new JobbaAsyncDuplicateLock();
            return locker.LockAsync(jobId, cancellationToken);
        }
    }
}
