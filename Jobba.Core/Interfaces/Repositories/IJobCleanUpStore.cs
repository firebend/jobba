using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Repositories;

public interface IJobCleanUpStore
{
    Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken);
}
