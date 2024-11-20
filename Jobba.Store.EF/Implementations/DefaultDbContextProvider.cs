using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Interfaces;

namespace Jobba.Store.EF.Implementations;

public class DefaultDbContextProvider(JobbaDbContext context, IJobbaDbInitializer? jobbaDbInitializer = null) : IDbContextProvider
{
    public async Task<JobbaDbContext> GetDbContextAsync(CancellationToken cancellationToken)
    {
        if (jobbaDbInitializer != null)
        {
            await jobbaDbInitializer.InitializeAsync(cancellationToken);
        }
        return context;
    }
}
