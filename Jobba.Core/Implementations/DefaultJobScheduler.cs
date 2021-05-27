using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Implementations
{
    public class DefaultJobScheduler : IJobScheduler
    {
        private readonly IJobEventPublisher _publisher;
        private readonly IJobStore _jobStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly IJobCancellationTokenStore _jobCancellationTokenStore;

        public DefaultJobScheduler(IJobEventPublisher publisher,
            IJobStore jobStore,
            IServiceProvider serviceProvider,
            IJobCancellationTokenStore jobCancellationTokenStore)
        {
            _publisher = publisher;
            _jobStore = jobStore;
            _serviceProvider = serviceProvider;
            _jobCancellationTokenStore = jobCancellationTokenStore;
        }

        public async Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken)
        {
            JobInfo<TJobParams, TJobState> jobInfo;

            if (request.IsRestart && request.JobId != Guid.Empty)
            {
                jobInfo = await _jobStore.SetJobAttempts<TJobParams, TJobState>(request.JobId, request.NumberOfTries, cancellationToken);
            }
            else
            {
                jobInfo = await _jobStore.AddJobAsync(request, cancellationToken);
            }

            var jobId = jobInfo.Id;

            await _jobStore.SetJobStatusAsync(jobId, JobStatus.Enqueued, DateTimeOffset.UtcNow, cancellationToken);

            var token = _jobCancellationTokenStore.CreateJobCancellationToken(jobId, cancellationToken);

            var watchEvent = new JobWatchEvent(jobId, typeof(TJobParams).AssemblyQualifiedName, typeof(TJobState).AssemblyQualifiedName);
            await _publisher.PublishWatchJobEventAsync(watchEvent, request.JobWatchInterval, cancellationToken);

            using var scope = _serviceProvider.CreateScope();

            if (scope.ServiceProvider.GetService(request.JobType) is not IJob<TJobParams, TJobState> job)
            {
                throw new Exception($"Could not resolve job from service provider. Job Type {request.JobType}");
            }

            var context = new JobStartContext<TJobParams, TJobState>
            {
                JobId = jobInfo.Id,
                JobParameters = request.JobParameters,
                JobState = request.InitialJobState,
                StartTime = jobInfo.EnqueuedTime
            };

            var _ = Task.Run(async () =>
            {
                try
                {
                    await _jobStore.SetJobStatusAsync(jobId, JobStatus.InProgress, DateTimeOffset.UtcNow, cancellationToken);
                    await job.StartAsync(context, token);
                    await _jobStore.SetJobStatusAsync(jobId, JobStatus.Completed, DateTimeOffset.UtcNow, cancellationToken);
                    await _publisher.PublishJobCompletedEventAsync(new JobCompletedEvent(jobId), cancellationToken);
                }
                catch (Exception ex)
                {
                    await _jobStore.LogFailureAsync(jobId, ex, cancellationToken);
                    await _publisher.PublishJobFaultedEventAsync(new JobFaultedEvent(jobId), cancellationToken);
                }
            }, token);

            await _publisher.PublishJobStartedEvent(new JobStartedEvent(jobId), token);

            return jobInfo;
        }

        public Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken)
            => _publisher.PublishJobCancellationRequestAsync(new CancelJobEvent(jobId), cancellationToken);
    }
}
