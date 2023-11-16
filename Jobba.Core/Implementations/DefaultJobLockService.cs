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

    private static readonly AsyncKeyedLocker<string> AsyncKeyedSystemLocker = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken)
        => AsyncKeyedLocker.LockAsync(jobId, cancellationToken);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<SystemLockResult> LockSystemAsync(string systemMoniker, TimeSpan span, CancellationToken cancellationToken)
    {
        var result = await AsyncKeyedSystemLocker.LockAsync(systemMoniker, span, cancellationToken);

        return new(result.EnteredSemaphore is false, result);
    }
}
