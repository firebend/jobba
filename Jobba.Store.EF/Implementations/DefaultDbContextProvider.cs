using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Implementations;

public class DefaultDbContextProvider(IServiceProvider serviceProvider, IJobbaDbInitializer? jobbaDbInitializer = null) : IDbContextProvider
{
    public async Task<JobbaDbContext> GetDbContextAsync(CancellationToken cancellationToken)
    {
        var context = serviceProvider.GetRequiredService<JobbaDbContext>();
        if (jobbaDbInitializer != null)
        {
            await jobbaDbInitializer.InitializeAsync(context, cancellationToken);
        }
        return context;
    }
}
