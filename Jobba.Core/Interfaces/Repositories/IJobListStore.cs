using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories
{
    //todo: impl
    public interface IJobListStore
    {
        Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken);

        Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken);
    }
}
