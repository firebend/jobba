using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations;

public class DefaultJobLockService : IJobLockService
{
    private static readonly AsyncKeyedLocker<Guid> AsyncKeyedLocker = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken) => AsyncKeyedLocker.LockAsync(jobId, cancellationToken);
}
