using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Store.EF.Interfaces;

public interface IJobbaDbInitializer
{
    public Task InitializeAsync(IJobbaDbContext context, CancellationToken cancellationToken);
}
