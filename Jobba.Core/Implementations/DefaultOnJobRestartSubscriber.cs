using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations
{
    public class DefaultOnJobRestartSubscriber : IOnJobRestartSubscriber
    {
        private readonly IJobLockService _jobLockService;
        private readonly IJobStore _jobStore;
        private readonly IJobScheduler _jobScheduler;

        public DefaultOnJobRestartSubscriber(IJobLockService jobLockService, IJobStore jobStore, IJobScheduler jobScheduler)
        {
            _jobLockService = jobLockService;
            _jobStore = jobStore;
            _jobScheduler = jobScheduler;
        }

        public async Task OnJobRestartAsync(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
        {
            using var _ = await _jobLockService.LockJobAsync(jobRestartEvent.JobId, cancellationToken);

            var method = this.GetType().GetMethod(nameof(RestartJob));

            if (method == null)
            {
                return;
            }

            var genericMethod = method.MakeGenericMethod(Type.GetType(jobRestartEvent.JobParamsTypeName), Type.GetType(jobRestartEvent.JobStateTypeName));

            var restartJobTaskAsObject = genericMethod.Invoke(this, new object[]
            {
                jobRestartEvent.JobId,
                cancellationToken
            });

            if (restartJobTaskAsObject is Task restartJobTask)
            {
                await restartJobTask;
            }
        }

        public async Task RestartJob<TParams, TState>(Guid jobId, CancellationToken cancellationToken)
        {
            var job = await _jobStore.GetJobByIdAsync<TParams, TState>(jobId, cancellationToken);

            if (job == null)
            {
                return;
            }

            if (job.Status != JobStatus.Faulted)
            {
                return;
            }

            if (job.CurrentNumberOfTries >= job.MaxNumberOfTries)
            {
                return;
            }

            var request = JobRequest<TParams, TState>.FromJobInfo(job);

            await _jobScheduler.ScheduleJobAsync(request, cancellationToken);
        }
    }
}
