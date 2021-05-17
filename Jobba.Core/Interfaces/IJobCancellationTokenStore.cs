using System;
using System.Threading;

namespace Jobba.Core.Interfaces
{
    public interface IJobCancellationTokenStore
    {
        CancellationTokenSource CreateJobCancellationToken(Guid jobId, CancellationToken cancellationToken);

        bool CancelJob(Guid id, CancellationToken cancellationToken);
    }
}
