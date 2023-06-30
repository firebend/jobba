using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobReScheduler : IJobReScheduler
{
    private readonly IJobEventPublisher _jobEventPublisher;
    private readonly IJobListStore _jobListStore;
    private readonly ILogger<DefaultJobReScheduler> _logger;

    public DefaultJobReScheduler(IJobListStore jobListStore,
        IJobEventPublisher jobEventPublisher,
        ILogger<DefaultJobReScheduler> logger)
    {
        _jobListStore = jobListStore;
        _jobEventPublisher = jobEventPublisher;
        _logger = logger;
    }

    public async Task RestartFaultedJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _jobListStore.GetJobsToRetry(cancellationToken);
        var jobsArray = jobs ?? new JobInfoBase[0];

        var tasks = jobsArray
            .Select(job =>
            {
                _logger.LogDebug("Restarting job. JobId: {JobId} Description: {JobDescription}", job.Id, job.Description);

                return _jobEventPublisher
                    .PublishJobRestartEvent(
                        new JobRestartEvent
                        {
                            JobId = job.Id,
                            JobParamsTypeName = job.JobParamsTypeName,
                            JobStateTypeName = job.JobStateTypeName
                        },
                        cancellationToken);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }
}
