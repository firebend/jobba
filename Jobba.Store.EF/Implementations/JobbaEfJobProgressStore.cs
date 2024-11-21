using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfJobProgressStore(
    IDbContextProvider dbContextProvider,
    IJobEventPublisher jobEventPublisher,
    IJobbaGuidGenerator guidGenerator)
    : IJobProgressStore
{
    public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress,
        CancellationToken cancellationToken)
        where TJobState : IJobState
    {
        var entity = JobProgressEntity.FromJobProgress(jobProgress);
        entity.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var job = await dbContext.Jobs.FindAsync([jobProgress.JobId], cancellationToken);

        if (job == null)
        {
            throw new InvalidOperationException($"Job with id {jobProgress.JobId} not found.");
        }

        entity.JobRegistrationId = job.JobRegistrationId;
        dbContext.JobProgress.Add(entity);

        job.JobState = jobProgress.JobState;
        job.LastProgressDate = jobProgress.Date;
        job.LastProgressPercentage = jobProgress.Progress;

        await dbContext.SaveChangesAsync(cancellationToken);

        await jobEventPublisher.PublishJobProgressEventAsync(
            new JobProgressEvent(entity.Id, entity.JobId, entity.JobRegistrationId),
            cancellationToken);
    }

    public async Task<JobProgressEntity?> GetProgressById(Guid id, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        return await dbContext.JobProgress.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
