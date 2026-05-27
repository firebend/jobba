using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobba.Core.Implementations;

public class DefaultJobReScheduler : IJobReScheduler
{
    private readonly IJobEventPublisher _jobEventPublisher;
    private readonly IJobListStore _jobListStore;
    private readonly IJobStore _jobStore;
    private readonly ILogger<DefaultJobReScheduler> _logger;
    private readonly JobbaCoreOptions _options;

    public DefaultJobReScheduler(IJobListStore jobListStore,
        IJobStore jobStore,
        IJobEventPublisher jobEventPublisher,
        ILogger<DefaultJobReScheduler> logger,
        IOptions<JobbaCoreOptions> options)
    {
        _jobListStore = jobListStore;
        _jobStore = jobStore;
        _jobEventPublisher = jobEventPublisher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RestartFaultedJobsAsync(CancellationToken cancellationToken)
    {
        // Snapshot jobs to retry before reclaiming orphans so that newly-reclaimed jobs are not
        // restarted in the same cycle (they will be picked up by the next scheduler cycle instead).
        var jobs = await _jobListStore.GetJobsToRetry(cancellationToken);

        // Reclaim orphaned InProgress jobs (those whose LastHeartbeatTime is older than JobWatchInterval × StaleMultiplier)
        var reclaimedCount = await _jobStore.ReclaimOrphanedJobsAsync(_options.StaleMultiplier, cancellationToken);
        if (reclaimedCount > 0)
        {
            _logger.LogInformation("Reclaimed {ReclaimedCount} orphaned InProgress job(s)", reclaimedCount);
        }
        var jobsArray = jobs ?? Array.Empty<JobInfoBase>();

        var tasks = jobsArray
            .Select(job =>
            {
                _logger.LogDebug("Restarting job. JobId: {JobId} Description: {JobDescription}", job.Id, job.Description);

                return _jobEventPublisher
                    .PublishJobRestartEvent(
                        new JobRestartEvent(job.Id,
                            job.JobParamsTypeName,
                            job.JobStateTypeName,
                            job.JobRegistrationId
                        ),
                        cancellationToken);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }
}
