using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories
{
    public interface IJobProgressStore
    {
        Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken);
    }
}
