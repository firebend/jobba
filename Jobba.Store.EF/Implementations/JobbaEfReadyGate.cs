using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Store.EF.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfReadyGate(IServiceScopeFactory scopeFactory) : IJobbaReadyGate
{
    private readonly SemaphoreSlim _readyLock = new(1, 1);
    private volatile bool _isReady;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        if (_isReady)
        {
            return;
        }

        await _readyLock.WaitAsync(cancellationToken);

        try
        {
            if (_isReady)
            {
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var dbContextProvider = scope.ServiceProvider.GetRequiredService<IDbContextProvider>();
            _ = await dbContextProvider.GetDbContextAsync(cancellationToken);
            _isReady = true;
        }
        finally
        {
            _readyLock.Release();
        }
    }
}
