using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;

namespace Jobba.Core.Abstractions
{
    public abstract class AbstractJobBaseClass<TJobParams, TJobState> : IJob<TJobParams, TJobState>
    {
        public abstract Task StartAsync(JobStartContext<TJobParams, TJobState> jobStartContext, CancellationToken cancellationToken);
    }
}
