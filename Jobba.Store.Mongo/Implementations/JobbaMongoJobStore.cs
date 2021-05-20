using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.AspNetCore.JsonPatch;

namespace Jobba.Store.Mongo.Implementations
{
    //todo: write test
    public class JobbaMongoJobStore : IJobStore
    {
        private readonly IMongoJobRepository<JobEntity> _jobRepository;

        public JobbaMongoJobStore(IMongoJobRepository<JobEntity> jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest,
            CancellationToken cancellationToken)
        {
            var added = await _jobRepository.AddAsync(JobEntity.FromRequest(jobRequest), cancellationToken);
            var info = added.ToJobInfo<TJobParams, TJobState>();
            return info;
        }

        public async Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken)
        {
            var patch = new JsonPatchDocument<JobEntity>();
            patch.Replace(x => x.CurrentNumberOfTries, attempts);

            var updated = await _jobRepository.UpdateAsync(jobId, patch, cancellationToken);
            var info = updated.ToJobInfo<TJobParams, TJobState>();
            return info;
        }

        public async Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date, CancellationToken cancellationToken)
        {
            var patch = new JsonPatchDocument<JobEntity>();
            patch.Replace(x => x.Status, status);
            patch.Replace(x => x.LastProgressDate, date);

            await _jobRepository.UpdateAsync(jobId, patch, cancellationToken);
        }

        public async Task LogProgressAsync<TJobParams, TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        {
            var jobId = jobProgress.JobId;
            var patch = new JsonPatchDocument<JobEntity>();
            var progressEntity = JobProgressEntity.FromJobProgress(jobProgress);
            patch.Add(x => x.Progresses, progressEntity);

            await _jobRepository.UpdateAsync(jobId, patch, cancellationToken);
        }

        public async Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken)
        {
            var patch = new JsonPatchDocument<JobEntity>();
            patch.Replace(x => x.FaultedReason, ex.ToString());
            patch.Replace(x => x.Status, JobStatus.Faulted);

            await _jobRepository.UpdateAsync(jobId, patch, cancellationToken);
        }

        public async Task<JobInfoBase> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            var entity = await _jobRepository.GetFirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

            var jobInfoBase = entity?.ToJobInfoBase();
            return jobInfoBase;
        }

        public async Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId, CancellationToken cancellationToken)
        {
            var entity = await _jobRepository.GetFirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

            var jobInfo = entity?.ToJobInfo<TJobParams, TJobState>();
            return jobInfo;
        }
    }
}
