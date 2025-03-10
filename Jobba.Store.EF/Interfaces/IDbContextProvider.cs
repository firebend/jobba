using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;

namespace Jobba.Store.EF.Interfaces;

public interface IDbContextProvider
{
    Task<JobbaDbContext> GetDbContextAsync(CancellationToken cancellationToken);
}
