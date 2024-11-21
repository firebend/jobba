using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class DefaultJobbaDbInitializer(ILogger<DefaultJobbaDbInitializer> logger) : IJobbaDbInitializer
{
    private static Task? _migrationTask;

    public async Task InitializeAsync(IJobbaDbContext context, CancellationToken cancellationToken)
    {
        if (_migrationTask is not null)
        {
            await _migrationTask;
            return;
        }
        try
        {
            _migrationTask = context.MigrateAsync(cancellationToken);
            await _migrationTask;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error migrating Jobba database.");
            throw;
        }
    }
}
