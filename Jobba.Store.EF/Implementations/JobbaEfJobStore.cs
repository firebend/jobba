using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfJobStore(
    IDbContextProvider dbContextProvider,
    IJobRegistrationStore jobRegistrationStore,
    IJobbaGuidGenerator guidGenerator,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaEfJobStore> logger)
    : IJobStore
{
    public async Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> jobRequest,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (string.IsNullOrWhiteSpace(jobRequest.JobName))
        {
            throw new ArgumentException("Job name cannot be null or whitespace.", nameof(jobRequest));
        }

        var jobRegistration = await jobRegistrationStore.GetByJobNameAsync(jobRequest.JobName, cancellationToken)
                              ?? throw new Exception($"Job registration not found for JobName {jobRequest.JobName}");

        var systemInfo = systemInfoProvider.GetSystemInfo();
        var job = JobEntity.FromRequest(jobRequest, jobRegistration.Id, systemInfo);

        if (job.Id == Guid.Empty)
        {
            job.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);
        }

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        dbContext.Jobs.Add(job);

        await dbContext.SaveChangesAsync(cancellationToken);

        var info = job.ToJobInfo<TJobParams, TJobState>();

        return info;
    }

    public async Task<JobInfo<TJobParams, TJobState>?> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        logger.LogDebug("Setting job {JobId} attempts to {Attempts}", jobId, attempts);
        var job = await GetJobFromDbAsync(jobId, false, cancellationToken);

        if (job == null)
        {
            return null;
        }

        job.CurrentNumberOfTries = attempts;

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return job.ToJobInfo<TJobParams, TJobState>();
    }

    public async Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job {JobId} status to {Status}", jobId, status);
        var job = await GetJobFromDbAsync(jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.Status = status;
        job.LastProgressDate = date;

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken)
    {
        logger.LogDebug("Logging failure for job {JobId}", jobId);
        var job = await GetJobFromDbAsync(jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.FaultedReason = ex.ToString();
        job.Status = JobStatus.Faulted;

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobInfoBase?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await GetJobFromDbAsync(jobId, true, cancellationToken);
        return job?.ToJobInfoBase();
    }

    public async Task<JobInfo<TJobParams, TJobState>?> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var job = await GetJobFromDbAsync(jobId, true, cancellationToken);
        return job?.ToJobInfo<TJobParams, TJobState>();
    }

    private async Task<JobEntity?> GetJobFromDbAsync(Guid jobId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var query = dbContext.Jobs.Where(x => x.Id == jobId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            logger.LogDebug("Job with id {JobId} not found.", jobId);
            return null;
        }

        return entity;
    }
}
