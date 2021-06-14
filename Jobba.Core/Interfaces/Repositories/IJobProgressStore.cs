using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Interfaces.Repositories
{
    public interface IJobProgressStore
    {
        Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken);

        //todo: impl and test
        //Task<JobProgressEntity> GetProgressById(Guid id, CancellationToken cancellationToken);
    }
}
