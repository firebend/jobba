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
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

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

        job.JobType ??= jobRegistration.JobType.AssemblyQualifiedName;

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
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return null;
        }

        job.CurrentNumberOfTries = attempts;

        await dbContext.SaveChangesAsync(cancellationToken);

        return job.ToJobInfo<TJobParams, TJobState>();
    }

    public async Task SetJobAttempts(Guid jobId, int attempts, CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job {JobId} attempts to {Attempts}", jobId, attempts);
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.CurrentNumberOfTries = attempts;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job {JobId} status to {Status}", jobId, status);
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.Status = status;
        job.LastProgressDate = date;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken)
    {
        logger.LogDebug("Logging failure for job {JobId}", jobId);
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.FaultedReason = ex.ToString();
        job.Status = JobStatus.Faulted;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobInfoBase?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, true, cancellationToken);
        return job?.ToJobInfoBase();
    }

    public async Task<JobInfo<TJobParams, TJobState>?> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, true, cancellationToken);
        return job?.ToJobInfo<TJobParams, TJobState>();
    }

    private async Task<JobEntity?> GetJobFromDbAsync(IJobbaDbContext dbContext, Guid jobId, bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.Where(x => x.Id == jobId && x.SystemInfo.SystemMoniker == _systemInfo.SystemMoniker);

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

    public async Task SetHeartbeatAsync(Guid jobId, DateTimeOffset heartbeatTime, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        job.LastHeartbeatTime = heartbeatTime;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ModifyJobAsync(Guid jobId, Action<JobEntity> action, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await GetJobFromDbAsync(dbContext, jobId, false, cancellationToken);

        if (job == null)
        {
            return;
        }

        action(job);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ReclaimOrphanedJobsAsync(int staleMultiplier, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var systemInfo = _systemInfo;

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);

        var candidates = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.SystemInfo.SystemMoniker == systemInfo.SystemMoniker
                        && x.Status == JobStatus.InProgress
                        && x.LastHeartbeatTime != null)
            .Select(x => new { x.Id, x.LastHeartbeatTime, x.JobWatchInterval })
            .ToListAsync(cancellationToken);

        var staleJobs = candidates
            .Where(x => x.LastHeartbeatTime!.Value.Add(x.JobWatchInterval * staleMultiplier) < now)
            .ToList();

        foreach (var job in staleJobs)
        {
            await ModifyJobAsync(job.Id,
                x =>
                {
                    x.Status = JobStatus.Faulted;
                    x.FaultedReason = JobbaCoreOptions.OrphanedJobFaultedReason;
                }, cancellationToken);
        }

        return staleJobs.Count;
    }
}
