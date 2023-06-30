using Jobba.Cron.Models;

namespace Jobba.Cron.Interfaces;

public interface ICronJobStateParamsProvider<TJobParams, TJobState>
{
    public CronJobStateParams<TJobParams, TJobState> GetParametersAndState();
}
