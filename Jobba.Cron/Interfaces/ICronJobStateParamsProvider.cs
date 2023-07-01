using Jobba.Cron.Models;

namespace Jobba.Cron.Interfaces;

public interface ICronJobStateParamsProvider<TJobParams, TJobState>
{
    /// <summary>
    /// Provides job parameters and state for a cron job.
    /// </summary>
    /// <returns>
    /// <see cref="CronJobStateParams{TJobParams,TJobState}"/>
    /// </returns>
    public CronJobStateParams<TJobParams, TJobState> GetParametersAndState();
}
