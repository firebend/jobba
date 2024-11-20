using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Implementations;

public class DefaultJobbaDbInitializer(JobbaDbContext context) : IJobbaDbInitializer
{
    private static bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);
    }
}
