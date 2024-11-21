using System.Threading;
using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;

namespace Jobba.Store.EF.Interfaces;

public interface IJobbaDbInitializer
{
    public Task InitializeAsync(IJobbaDbContext context, CancellationToken cancellationToken);
}
