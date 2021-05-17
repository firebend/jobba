using System;
using System.Threading;

namespace Jobba.Core.Interfaces
{
    public interface IJobCancellationTokenStore
    {
        CancellationToken CreateJobCancellationToken(Guid jobId, CancellationToken cancellationToken);

        bool CancelJob(Guid id);
    }
}
