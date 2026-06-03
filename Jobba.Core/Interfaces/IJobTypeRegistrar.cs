using Jobba.Core.Builders;

namespace Jobba.Core.Interfaces;

public interface IJobTypeRegistrar
{
    public void OnJobAdded<TJob, TJobParams, TJobState>(JobbaBuilder builder)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
}
