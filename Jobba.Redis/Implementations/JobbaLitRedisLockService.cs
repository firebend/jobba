using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Redis.Implementations;

public class LitRedisJobLockService : IJobLockService
{
    private readonly ILogger<LitRedisJobLockService> _logger;
    private readonly ILitRedisDistributedLockService _lockService;

    public LitRedisJobLockService(ILitRedisDistributedLockService lockService, ILogger<LitRedisJobLockService> logger)
    {
        _lockService = lockService;
        _logger = logger;
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

    public async Task<SystemLockResult> LockSystemAsync(string systemMoniker, TimeSpan span, CancellationToken cancellationToken)
    {
        var key = $"Jobba_System_{systemMoniker}";

        var lockModel = RequestLockModel
            .WithKey(key)
            .WithLockWaitTimeout(span);

        var locker = await _lockService.AcquireLockAsync(lockModel, cancellationToken);

        _logger.LogDebug("Attempted to acquire lock {Key} {Succeeded}", systemMoniker, locker.Succeeded);

        return new(locker.Succeeded, locker);
    }

}
