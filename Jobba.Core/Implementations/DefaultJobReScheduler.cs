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
        // Reclaim orphaned InProgress jobs (those whose LastHeartbeatTime is older than JobWatchInterval × StaleMultiplier)
        var reclaimedCount = await _jobStore.ReclaimOrphanedJobsAsync(_options.StaleMultiplier, cancellationToken);
        if (reclaimedCount > 0)
        {
            _logger.LogInformation("Reclaimed {ReclaimedCount} orphaned InProgress job(s)", reclaimedCount);
        }

        var jobs = await _jobListStore.GetJobsToRetry(cancellationToken) ?? [];

        var tasks = jobs
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
