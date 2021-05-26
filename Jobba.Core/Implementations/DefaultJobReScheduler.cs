using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations
{
    public class DefaultJobReScheduler : IJobReScheduler
    {
        private readonly IJobEventPublisher _jobEventPublisher;
        private readonly IJobListStore _jobListStore;

        public DefaultJobReScheduler(IJobListStore jobListStore,
            IJobEventPublisher jobEventPublisher)
        {
            _jobListStore = jobListStore;
            _jobEventPublisher = jobEventPublisher;
        }

        public async Task RestartFaultedJobsAsync(CancellationToken cancellationToken)
        {
            var jobs = await _jobListStore.GetJobsToRetry(cancellationToken);
            var jobsArray = jobs ?? new JobInfoBase[0];

            var tasks = jobsArray
                .Select(job => _jobEventPublisher
                    .PublishJobRestartEvent(
                        new JobRestartEvent
                        {
                            JobId = job.Id,
                            JobParamsTypeName = job.JobParamsTypeName,
                            JobStateTypeName = job.JobStateTypeName
                        },
                        cancellationToken))
                .ToList();

            await Task.WhenAll(tasks);
        }
    }
}
