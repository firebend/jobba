namespace Jobba.Cron.Models;

public record CronJobStateParams<TJobParams, TJobState>
{
    public TJobParams Parameters { get; set; }

    public TJobState State { get; set; }
}
