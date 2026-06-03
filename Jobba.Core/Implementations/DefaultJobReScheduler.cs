using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobba.Core.Implementations;

public class DefaultJobReScheduler : IJobReScheduler
{
    private readonly IJobEventDispatcher _dispatcher;
    private readonly IJobListStore _jobListStore;
    private readonly IJobStore _jobStore;
    private readonly ILogger<DefaultJobReScheduler> _logger;
    private readonly JobbaCoreOptions _options;

    public DefaultJobReScheduler(IJobListStore jobListStore,
        IJobStore jobStore,
        IJobEventDispatcher dispatcher,
        ILogger<DefaultJobReScheduler> logger,
        IOptions<JobbaCoreOptions> options)
    {
        _jobListStore = jobListStore;
        _jobStore = jobStore;
        _dispatcher = dispatcher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RestartFaultedJobsAsync(CancellationToken cancellationToken)
    {
        // Reclaim orphaned InProgress jobs (those whose LastHeartbeatTime is older than JobWatchInterval × StaleMultiplier)
        var reclaimedCount = await _jobStore.ReclaimOrphanedJobsAsync(_options.StaleMultiplier, cancellationToken);
        if (reclaimedCount > 0)
        {
            _logger.LogInformation("Reclaimed {ReclaimedCount} orphaned InProgress job(s)", reclaimedCount);
        }

        var jobs = await _jobListStore.GetJobsToRetry(cancellationToken) ?? [];

        var tasks = jobs
            .Select(async job =>
            {
                _logger.LogDebug("Restarting job. JobId: {JobId} Description: {JobDescription}", job.Id, job.Description);

                var jobType = Type.GetType(job.JobTypeName);
                var paramsType = Type.GetType(job.JobParamsTypeName);
                var stateType = Type.GetType(job.JobStateTypeName);
                if (jobType is null || paramsType is null || stateType is null)
                {
                    _logger.LogError("Could not find job type, params type, or state type for job {JobId}", job.Id);
                    // update the job attempts so if a faulted job gets removed or renamed
                    // we don't end up attempting to retry it every time jobba starts up forever
                    await _jobStore.SetJobAttempts(job.Id, job.CurrentNumberOfTries + 1, cancellationToken);
                    return;
                }

                await _dispatcher.PublishRestartAsync(jobType, job.Id,
                    paramsType, stateType,
                    job.JobRegistrationId, cancellationToken);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

}
