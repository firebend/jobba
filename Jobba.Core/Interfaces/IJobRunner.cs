using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

public interface IJobRunner
{
    public Task RunJobAsync<TJobParams, TJobState>(
        IJob<TJobParams, TJobState> job,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken cancellationToken) where TJobParams : IJobParams where TJobState : IJobState;
}
