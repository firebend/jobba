using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    public interface IJobWatcher
    {
        Task WatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
