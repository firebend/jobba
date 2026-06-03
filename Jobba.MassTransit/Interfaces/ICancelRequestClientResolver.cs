using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Models;

namespace Jobba.MassTransit.Interfaces;

public interface ICancelRequestClientResolver
{
    Task<JobbaMassTransitJobCancelRequestResult<TJob>> RequestCancellationAsync<TJob, TJobParams, TJobState>(
        CancelJobEvent<TJob> cancelJobEvent,
        CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState;
}
