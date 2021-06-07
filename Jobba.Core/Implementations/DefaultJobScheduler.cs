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
    public class DefaultJobScheduler : IJobScheduler, IDisposable
    {
        private readonly IJobCancellationTokenStore _jobCancellationTokenStore;
        private readonly IJobStore _jobStore;
        private readonly IJobEventPublisher _publisher;
        private readonly IServiceProvider _serviceProvider;
        private readonly IJobbaGuidGenerator _guidGenerator;
        private readonly IJobLockService _lockService;

        private IServiceScope _serviceScope;
        private IServiceScope ServiceScope => _serviceScope ??= _serviceProvider.CreateScope();

        public DefaultJobScheduler(IJobEventPublisher publisher,
            IJobStore jobStore,
            IServiceProvider serviceProvider,
            IJobCancellationTokenStore jobCancellationTokenStore,
            IJobbaGuidGenerator guidGenerator,
            IJobLockService lockService)
        {
            _publisher = publisher;
            _jobStore = jobStore;
            _serviceProvider = serviceProvider;
            _jobCancellationTokenStore = jobCancellationTokenStore;
            _guidGenerator = guidGenerator;
            _lockService = lockService;
        }

        public async Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(
            JobRequest<TJobParams, TJobState> request,
            CancellationToken cancellationToken)
        {
            var jobId = await GetJobIdAsync(request, cancellationToken);

            if (! await CanRunAsync(jobId, cancellationToken))
            {
                return null;
            }

            using var jobLock = await _lockService.LockJobAsync(jobId, cancellationToken);

            if (! await CanRunAsync(jobId, cancellationToken))
            {
                return null;
            }

            var jobInfo = await UpdateAttemptsOrCreateJobAsync(request, cancellationToken);
            await _jobStore.SetJobStatusAsync(jobId, JobStatus.Enqueued, DateTimeOffset.UtcNow, cancellationToken);
            var token = _jobCancellationTokenStore.CreateJobCancellationToken(jobId, cancellationToken);
            await WatchJobAsync<TJobParams, TJobState>(jobId, request.JobWatchInterval, token);
            var context = GetJobStartContext(request, jobInfo);
            var _ = RunJobAsync(jobId, request.JobType, context, token);
            await NotifyJobStartedAsync<TJobParams, TJobState>(jobId, token);

            return jobInfo;
        }

        private async Task<bool> CanRunAsync(Guid jobId, CancellationToken cancellationToken)
        {
            if (jobId == Guid.Empty)
            {
                return true;
            }

            var existingJob = await _jobStore.GetJobByIdAsync(jobId, cancellationToken);

            return existingJob?.Status is not (JobStatus.Enqueued or JobStatus.InProgress);
        }

        private async Task<Guid> GetJobIdAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken)
        {
            if (!request.IsRestart || request.JobId == Guid.Empty)
            {
                var newGuid = await _guidGenerator.GenerateGuidAsync(cancellationToken);
                request.JobId = newGuid;
            }

            return request.JobId;
        }

        private async Task NotifyJobStartedAsync<TJobParams, TJobState>(Guid jobId, CancellationToken token)
            => await _publisher.PublishJobStartedEvent(new JobStartedEvent(jobId), token);

        private async Task WatchJobAsync<TJobParams, TJobState>(Guid jobId, TimeSpan watchInterval, CancellationToken cancellationToken)
        {
            var watchEvent = new JobWatchEvent(jobId, typeof(TJobParams).AssemblyQualifiedName, typeof(TJobState).AssemblyQualifiedName);
            await _publisher.PublishWatchJobEventAsync(watchEvent, watchInterval, cancellationToken);
        }

        private async Task<JobInfo<TJobParams, TJobState>> UpdateAttemptsOrCreateJobAsync<TJobParams, TJobState>(
            JobRequest<TJobParams, TJobState> request,
            CancellationToken cancellationToken)
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

            return jobInfo;
        }

        public Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken)
            => _publisher.PublishJobCancellationRequestAsync(new CancelJobEvent(jobId), cancellationToken);

        private async Task RunJobAsync<TJobParams, TJobState>(Guid jobId,
            Type jobType,
            JobStartContext<TJobParams, TJobState> context,
            CancellationToken cancellationToken)
        {
            if (ServiceScope.ServiceProvider.GetService(jobType) is not IJob<TJobParams, TJobState> job)
            {
                throw new Exception($"Could not resolve job from service provider. Job Type {jobType}");
            }

            await Task.Run(async () =>
            {
                try
                {
                    await _jobStore.SetJobStatusAsync(jobId, JobStatus.InProgress, DateTimeOffset.UtcNow, cancellationToken);
                    await job.StartAsync(context, cancellationToken);
                    await _jobStore.SetJobStatusAsync(jobId, JobStatus.Completed, DateTimeOffset.UtcNow, cancellationToken);
                    await _publisher.PublishJobCompletedEventAsync(new JobCompletedEvent(jobId), cancellationToken);
                }
                catch (Exception ex)
                {
                    await _jobStore.LogFailureAsync(jobId, ex, cancellationToken);
                    await _publisher.PublishJobFaultedEventAsync(new JobFaultedEvent(jobId), cancellationToken);
                }
            }, cancellationToken);
        }

        private static JobStartContext<TJobParams, TJobState> GetJobStartContext<TJobParams, TJobState>(
            JobRequest<TJobParams, TJobState> request,
            JobInfoBase jobInfo) => new()
        {
            JobId = jobInfo.Id,
            JobParameters = request.JobParameters,
            JobState = request.InitialJobState,
            StartTime = jobInfo.EnqueuedTime,
            IsRestart = request.IsRestart,
            LastProgressDate = jobInfo.LastProgressDate,
            LastProgressPercentage = jobInfo.LastProgressPercentage,
            CurrentNumberOfTries = request.NumberOfTries
        };

        public void Dispose() => _serviceScope?.Dispose();
    }
}
