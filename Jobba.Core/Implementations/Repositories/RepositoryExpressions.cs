using System;
using System.Linq.Expressions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Implementations.Repositories;

public static class RepositoryExpressions
{
    public static readonly Expression<Func<JobEntity, bool>> JobRetryExpression =
        x => (x.Status == JobStatus.Faulted || x.Status == JobStatus.ForceCancelled || x.Status == JobStatus.Unknown) &&
             !x.IsOutOfRetry;

    public static readonly Expression<Func<JobEntity, bool>> JobsInProgressExpression =
        x => x.Status == JobStatus.InProgress || x.Status == JobStatus.Enqueued;

    public static Expression<Func<JobEntity, bool>> GetCleanUpExpression(DateTimeOffset date)
    {
        Expression<Func<JobEntity, bool>> filter =
            x => x.Status != JobStatus.Enqueued &&
                 x.Status != JobStatus.InProgress &&
                 date.CompareTo(x.LastProgressDate) > 0;

        return filter;
    }
}
