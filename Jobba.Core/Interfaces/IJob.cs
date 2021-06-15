using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces
{
    public interface IJob
    {
        string JobName { get; }
    }

    public interface IJob<TJobParams, TJobState> : IJob
    {
        Task StartAsync(JobStartContext<TJobParams, TJobState> jobStartContext, CancellationToken cancellationToken);
    }
}
