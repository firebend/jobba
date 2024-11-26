using System;
using System.Linq.Expressions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Implementations.Repositories;

public static class RepositoryExpressions
{
    public static Expression<Func<JobEntity, bool>> JobRetryExpression(JobSystemInfo systemInfo)
    {
        Expression<Func<JobEntity, bool>> filter = x => x.SystemInfo.SystemMoniker == systemInfo.SystemMoniker &&
                                                        (x.Status == JobStatus.Faulted ||
                                                         x.Status == JobStatus.ForceCancelled ||
                                                         x.Status == JobStatus.Unknown) &&
                                                        !x.IsOutOfRetry;
        return filter;
    }

    public static Expression<Func<JobEntity, bool>> JobsInProgressExpression(JobSystemInfo systemInfo)
    {
        Expression<Func<JobEntity, bool>> filter = x =>
            x.SystemInfo.SystemMoniker == systemInfo.SystemMoniker &&
            (x.Status == JobStatus.InProgress || x.Status == JobStatus.Enqueued);
        return filter;
    }

    public static Expression<Func<JobRegistration, bool>> GetCronJobRegistrationsExpression(
        JobSystemInfo systemInfo)
    {
        Expression<Func<JobRegistration, bool>> filter =
            x => x.CronExpression != null &&
                 x.CronExpression != "" &&
                 x.SystemMoniker == systemInfo.SystemMoniker &&
                 x.IsInactive != true;

        return filter;
    }

    public static Expression<Func<JobRegistration, bool>> GetJobByNameExpression(
        JobSystemInfo systemInfo, string jobName)
    {
        Expression<Func<JobRegistration, bool>> filter =
            x => x.SystemMoniker == systemInfo.SystemMoniker &&
                 x.JobName == jobName;

        return filter;
    }

    public static Expression<Func<JobEntity, bool>> GetCleanUpExpression(JobSystemInfo systemInfo,
        DateTimeOffset date)
    {
        Expression<Func<JobEntity, bool>> filter =
            x => x.SystemInfo.SystemMoniker == systemInfo.SystemMoniker &&
                 x.Status != JobStatus.Enqueued &&
                 x.Status != JobStatus.InProgress &&
                 date.CompareTo(x.LastProgressDate) > 0;

        return filter;
    }
}
