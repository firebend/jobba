using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Models;

namespace Jobba.MassTransit.Interfaces;

public interface ICancelRequestClientResolver
{
    Task<JobbaMassTransitJobCancelRequestResult<TJob>> RequestCancellationAsync<TJob, TJobParams, TJobState>(
        Guid jobId,
        Guid jobRegistrationId,
        CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
}
