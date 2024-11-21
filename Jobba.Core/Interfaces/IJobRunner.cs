using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

public interface IJobRunner
{
    Task RunJobAsync<TJobParams, TJobState>(
        IJob<TJobParams, TJobState> job,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken jobCancellationToken,
        CancellationToken cancellationToken) where TJobParams : IJobParams where TJobState : IJobState;
}
