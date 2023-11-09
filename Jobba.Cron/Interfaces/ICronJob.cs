using Jobba.Core.Interfaces;

namespace Jobba.Cron.Interfaces;

public interface ICronJob : IJob
{
}

public interface ICronJob<TJobParams, TJobState> : ICronJob, IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
}
