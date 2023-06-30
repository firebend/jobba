using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;

namespace Jobba.Redis.Implementations;

public class LitRedisJobLockService : IJobLockService
{
    private readonly ILitRedisDistributedLockService _lockService;

    public LitRedisJobLockService(ILitRedisDistributedLockService lockService)
    {
        _lockService = lockService;
    }

    public async ValueTask<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var lockModel = RequestLockModel
            .WithKey($"Jobba_{jobId}")
            .WaitForever();

        var locker = await _lockService.AcquireLockAsync(lockModel, cancellationToken);

        if (locker.Succeeded)
        {
            return locker;
        }

        throw new Exception($"Could not acquire lock. Job Id {jobId}");
    }
}
