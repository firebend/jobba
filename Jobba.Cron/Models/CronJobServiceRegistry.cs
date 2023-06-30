using System;

namespace Jobba.Cron.Models;

public class CronJobServiceRegistry
{
    public string Cron { get; set; }

    public Type JobType { get; set; }

    public string JobName { get; set; }

    public Type JobParamsType { get; set; }

    public Type JobStateType { get; set; }

    public string Description { get; set; }

    public TimeSpan WatchInterval { get; set; } = TimeSpan.FromSeconds(30);
}
