using Jobba.Cron.Interfaces;
using Jobba.Cron.Models;

namespace Jobba.Cron.Implementations;

public class DefaultCronJobStateParamsProvider<TJobParams, TJobState> : ICronJobStateParamsProvider<TJobParams, TJobState>
    where TJobParams : class, new()
    where TJobState : class, new()
{
    public TJobParams JobParams { get; set; } = new();

    public TJobState JobState { get; set; } = new();

    public CronJobStateParams<TJobParams, TJobState> GetParametersAndState() => new()
    {
        Parameters = JobParams,
        State = JobState
    };
}
