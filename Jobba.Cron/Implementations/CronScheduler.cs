using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;
using Jobba.Cron.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jobba.Cron.Implementations;

public record CronJobWithRegistry(ICronJob CronJob, CronJobServiceRegistry Registry, DateTimeOffset[] NextExecutionDate);

public static class CronSchedulerMap
{
    private static readonly Dictionary<DateTimeOffset, List<CronJobWithRegistry>> Map = new();

    public static void Upsert(CronJobWithRegistry registry, DateTimeOffset cleanUpDate)
    {
        foreach (var key in Map.Keys.Where(key => key < cleanUpDate))
        {
            Map.Remove(key);
        }

        foreach (var next in registry.NextExecutionDate)
        {
            if (Map.TryGetValue(next, out var list))
            {
                list.Add(registry);
                return;
            }

            Map[next] = new() { registry };
        }
    }

    public static IEnumerable<CronJobWithRegistry> GetJobIntersection(DateTimeOffset start, DateTimeOffset end)
    {
        foreach (var (key, values) in Map)
        {
            if (!key.IsBetween(start, end))
            {
                continue;
            }

            foreach (var value in values)
            {
                yield return value;
            }
        }
    }

    public static void PrintSchedule(ILogger logger, LogLevel logLevel = LogLevel.Debug)
    {
        void NewLine(StringBuilder builder1)
        {
            builder1.Append(Environment.NewLine);
        }

        void Tab(StringBuilder stringBuilder)
        {
            stringBuilder.Append("   ");
        }

        if (logger.IsEnabled(logLevel) is false)
        {
            return;
        }

        var builder = new StringBuilder();

        foreach (var (key, values) in Map)
        {
            Tab(builder);
            builder.Append("Date: ");
            builder.Append(key.ToString());
            NewLine(builder);

            foreach(var value in values)
            {
                Tab(builder);
                Tab(builder);

                builder.Append(value.CronJob.JobName);
                NewLine(builder);

                Tab(builder);
                Tab(builder);
                Tab(builder);

                foreach (var o in value.NextExecutionDate)
                {
                    builder.Append(o.ToString());
                    NewLine(builder);
                }
            }
        }

        logger.Log(logLevel, builder.ToString());
    }
}

public class CronScheduler : ICronScheduler
{
    private readonly ICronService _cronService;
    private readonly IEnumerable<CronJobServiceRegistry> _registries;
    private readonly IJobScheduler _scheduler;
    private readonly ILogger<CronScheduler> _logger;

    public CronScheduler(ICronService cronService, IEnumerable<CronJobServiceRegistry> registries, IJobScheduler scheduler, ILogger<CronScheduler> logger)
    {
        _cronService = cronService;
        _registries = registries;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task EnqueueJobsAsync(IServiceScope scope, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        foreach (var job in GetJobsNeedingInvoking(scope, start, end))
        {
            _logger.LogDebug("Job is set for execution. {JobName} {CronExpression} {Start} {End}", job.CronJob.JobName, job.Registry.Cron, start, end);

            var methodInfo = typeof(CronScheduler)
                .GetMethod(nameof(EnqueueJobAsync), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(job.Registry.JobParamsType, job.Registry.JobStateType);

            if (methodInfo is null)
            {
                _logger.LogCritical("Could not find method info for {Method}", nameof(EnqueueJobAsync));
                return;
            }

            var enqueueTask = methodInfo.Invoke(this, new object[]
                {
                    _scheduler,
                    scope,
                    job,
                    cancellationToken
                });

            if(enqueueTask is Task task)
            {
                await task;
            }
        }
    }

    private IEnumerable<CronJobWithRegistry> GetJobsNeedingInvoking(IServiceScope scope, DateTimeOffset start, DateTimeOffset end)
    {
        foreach (var registry in _registries)
        {
            var service = scope.ServiceProvider.GetService(registry.JobType);

            if (service is not ICronJob job)
            {
                continue;
            }

            var nextExecution = _cronService.GetSchedule(registry.Cron, start, end);
            var cronRegistry = new CronJobWithRegistry(job, registry, nextExecution);
            CronSchedulerMap.Upsert(cronRegistry, start);
        }

        return CronSchedulerMap.GetJobIntersection(start, end);
    }

    private static async Task EnqueueJobAsync<TJobParams, TJobState>(IJobScheduler jobScheduler, IServiceScope scope, CronJobWithRegistry job, CancellationToken cancellationToken)
    {
        var parametersAndState = scope.ServiceProvider
            .GetService<ICronJobStateParamsProvider<TJobParams, TJobState>>()
            ?.GetParametersAndState() ?? new();

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
