using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Cron.Interfaces;
using Jobba.Cron.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.Implementations;

public record CronJobWithRegistry(ICronJob CronJob, CronJobServiceRegistry Registry);

public class CronScheduler : ICronScheduler
{
    private readonly ILogger<CronScheduler> _logger;
    private readonly IEnumerable<CronJobServiceRegistry> _registries;
    private readonly IEnumerable<JobRegistration> _jobRegistrations;
    private readonly IJobScheduler _scheduler;
    private readonly ICronService _cronService;

    public CronScheduler(IEnumerable<CronJobServiceRegistry> registries,
        IJobScheduler scheduler,
        ILogger<CronScheduler> logger,
        ICronService cronService,
        IEnumerable<JobRegistration> jobRegistrations)
    {
        _registries = registries;
        _scheduler = scheduler;
        _logger = logger;
        _cronService = cronService;
        _jobRegistrations = jobRegistrations;
    }

    public async Task EnqueueJobsAsync(IServiceScope scope, DateTimeOffset min, DateTimeOffset max, CancellationToken cancellationToken)
    {
        var tasks = GetJobsNeedingInvoking(scope, min, max)
            .Select(x => InvokeJobUsingReflectionAsync(scope, x, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task InvokeJobUsingReflectionAsync(IServiceScope scope, CronJobWithRegistry job, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job is set for execution. {JobName} {CronExpression} {Start}", job.CronJob.JobName, job.Registry.Cron, DateTimeOffset.UtcNow);

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
            _jobRegistrations,
            _logger,
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

    private IEnumerable<CronJobWithRegistry> GetJobsNeedingInvoking(IServiceScope scope, DateTimeOffset min, DateTimeOffset max)
    {
        foreach (var registry in _registries)
        {
            if (registry.NextExecutionDate is null)
            {
                registry.SetNextExecutionDate(_cronService, max);
            }

            var shouldExecute = registry.ShouldExecute(min, max);

            registry.SetNextExecutionDate(_cronService, max);

            if (shouldExecute is false)
            {
                continue;
            }

            var service = scope.ServiceProvider.Materialize(registry.JobType);

            if (service is not ICronJob job)
            {
                _logger.LogInformation("Job could not be materialized from service provider {Job}", registry.JobType);
                continue;
            }

            yield return new CronJobWithRegistry(job, registry);
        }
    }

    private static async Task EnqueueJobAsync<TJobParams, TJobState>(IJobScheduler jobScheduler,
        IEnumerable<JobRegistration> jobRegistrations,
        ILogger<CronScheduler> logger,
        IServiceScope scope,
        CronJobWithRegistry job,
        CancellationToken cancellationToken)
    {
        var parametersAndState = scope.ServiceProvider
            .GetService<ICronJobStateParamsProvider<TJobParams, TJobState>>()
            ?.GetParametersAndState() ?? new CronJobStateParams<TJobParams, TJobState>();

        var registration = jobRegistrations.FirstOrDefault(x => x.JobName == job.CronJob.JobName);

        if (registration is null)
        {
            logger.LogWarning("There is no registration for cron job {JobName}, job will not execute", job.CronJob.JobName);
            return;
        }

        var request = new JobRequest<TJobParams, TJobState>
        {
            JobType = job.CronJob.GetType(),
            Description = job.Registry.Description,
            IsRestart = false,
            JobId = Guid.NewGuid(),
            JobParameters = parametersAndState.Parameters,
            JobWatchInterval = job.Registry.WatchInterval,
            InitialJobState = parametersAndState.State,
            JobRegistrationId = registration.Id,
        };

        await jobScheduler.ScheduleJobAsync(request, cancellationToken);

        job.Registry.PreviousExecutionDate = DateTimeOffset.UtcNow;
    }
}
