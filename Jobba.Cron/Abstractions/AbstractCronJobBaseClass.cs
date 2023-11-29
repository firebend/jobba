using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Cron.Interfaces;

namespace Jobba.Cron.Abstractions;

public abstract class AbstractCronJobBaseClass<TJobParams, TJobState> : AbstractJobBaseClass<TJobParams, TJobState>, ICronJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    protected AbstractCronJobBaseClass(IJobProgressStore progressStore) : base(progressStore)
    {
    }
}
