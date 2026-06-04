using System;
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

public class JobbaEfJobProgressStore(
    IDbContextProvider dbContextProvider,
    IJobEventDispatcher dispatcher,
    IJobbaGuidGenerator guidGenerator,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaEfJobProgressStore> logger)
    : IJobProgressStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress,
        CancellationToken cancellationToken)
        where TJobState : IJobState
    {
        logger.LogDebug("Logging progress for job {jobId}: {progress}",
            jobProgress.JobId, jobProgress.Progress);

        var entity = JobProgressEntity.FromJobProgress(jobProgress);
        entity.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await dbContext.Jobs.FirstOrDefaultAsync(
                  x => x.Id == jobProgress.JobId && x.SystemInfo.SystemMoniker == _systemInfo.SystemMoniker,
                  cancellationToken) ??
                  throw new InvalidOperationException($"Job with id {jobProgress.JobId} not found.");

        entity.JobRegistrationId = job.JobRegistrationId;
        dbContext.JobProgress.Add(entity);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Publishing progress event for job {jobId}: {progress}",
            jobProgress.JobId, jobProgress.Progress);

        var jobType = Type.GetType(job.JobType);
        if (jobType is not null)
        {
            await dispatcher.PublishProgressAsync(jobType, entity.Id, entity.JobId, entity.JobRegistrationId, cancellationToken);
        }

        job.JobState = jobProgress.JobState;
        job.LastProgressDate = jobProgress.Date;
        job.LastProgressPercentage = jobProgress.Progress;

        logger.LogDebug("Updating job {jobId} with progress: {progress}",
            jobProgress.JobId, jobProgress.Progress);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobProgressEntity?> GetProgressById(Guid id, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        return await dbContext.JobProgress.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
