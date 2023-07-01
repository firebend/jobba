using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;
using Jobba.Cron.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.Implementations;

public record CronJobWithRegistry(ICronJob CronJob, CronJobServiceRegistry Registry);

public class CronScheduler : ICronScheduler
{
    private readonly ICronService _cronService;
    private readonly ILogger<CronScheduler> _logger;
    private readonly IEnumerable<CronJobServiceRegistry> _registries;
    private readonly IJobScheduler _scheduler;

    public CronScheduler(ICronService cronService,
        IEnumerable<CronJobServiceRegistry> registries,
        IJobScheduler scheduler,
        ILogger<CronScheduler> logger)
    {
        _cronService = cronService;
        _registries = registries;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task EnqueueJobsAsync(IServiceScope scope, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tasks = GetJobsNeedingInvoking(scope, now)
            .Select(x => InvokeJobUsingReflectionAsync(scope, now, x, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task InvokeJobUsingReflectionAsync(IServiceScope scope, DateTimeOffset now, CronJobWithRegistry job, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job is set for execution. {JobName} {CronExpression} {Start}", job.CronJob.JobName, job.Registry.Cron, now);

        var methodInfo = typeof(CronScheduler)
            .GetMethod(nameof(EnqueueJobAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(job.Registry.JobParamsType, job.Registry.JobStateType);

        if (methodInfo is null)
        {
            _logger.LogCritical("Could not find method info for {Method}", nameof(EnqueueJobAsync));
            return;
        }

        var methodInfoParameters = new object[]
        {
            _scheduler,
            scope,
            job,
            cancellationToken
        };

        var enqueueTask = methodInfo.Invoke(this, methodInfoParameters);

        if (enqueueTask is Task task)
        {
            await task;
        }
    }

    private IEnumerable<CronJobWithRegistry> GetJobsNeedingInvoking(IServiceScope scope, DateTimeOffset now)
    {
        foreach (var registry in _registries)
        {
            var nextExecutionDate = _cronService.GetNextExecutionDate(registry.Cron, now);

            var shouldExecute = nextExecutionDate.HasValue && nextExecutionDate.Value.TrimMilliseconds() == now;

            if (shouldExecute is false)
            {
                continue;
            }

            var service = scope.ServiceProvider.GetService(registry.JobType);

            if (service is not ICronJob job)
            {
                _logger.LogInformation("Job is either null or not registered in service collection {Job}", registry.JobType);
                continue;
            }

            yield return new CronJobWithRegistry(job, registry);
        }
    }

    private static async Task EnqueueJobAsync<TJobParams, TJobState>(IJobScheduler jobScheduler,
        IServiceScope scope,
        CronJobWithRegistry job,
        CancellationToken cancellationToken)
    {
        var parametersAndState = scope.ServiceProvider
            .GetService<ICronJobStateParamsProvider<TJobParams, TJobState>>()
            ?.GetParametersAndState() ?? new CronJobStateParams<TJobParams, TJobState>();

        var request = new JobRequest<TJobParams, TJobState>
        {
            JobType = job.CronJob.GetType(),
            Description = job.Registry.Description,
            IsRestart = false,
            JobId = Guid.NewGuid(),
            JobParameters = parametersAndState.Parameters,
            JobWatchInterval = job.Registry.WatchInterval,
            InitialJobState = parametersAndState.State
        };

        await jobScheduler.ScheduleJobAsync(request, cancellationToken);
    }
}
