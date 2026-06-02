using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations;

public class CancelRequestClientResolver : ICancelRequestClientResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JobbaMassTransitConfigurationContext _configurationContext;
    public CancelRequestClientResolver(
        IServiceProvider serviceProvider,
        JobbaMassTransitConfigurationContext configurationContext)
    {
        _serviceProvider = serviceProvider;
        _configurationContext = configurationContext;
    }

    public async Task<JobbaMassTransitJobCancelRequestResult<TJob>> RequestCancellationAsync<TJob, TJobParams, TJobState>(
        Guid jobId,
        Guid jobRegistrationId,
        CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var client = _serviceProvider.GetRequiredService<IRequestClient<CancelJobEvent<TJob>>>();
        var tries = 0;

        while (tries < _configurationContext.MaxTimesToRequestJobCancellation)
        {
            try
            {
                var response = await client.GetResponse<JobbaMassTransitJobCancelRequestResult<TJob>>(
                    new CancelJobEvent<TJob>(jobId, jobRegistrationId),
                    cancellationToken);

                if (response.Message.WasCancelled)
                {
                    return new JobbaMassTransitJobCancelRequestResult<TJob>
                    {
                        JobId = response.Message.JobId,
                        WasCancelled = true
                    };
                }

                await Task.Delay(_configurationContext.CancelJobRequestInterval, cancellationToken);
                tries++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(_configurationContext.CancelJobRequestInterval, cancellationToken);
                tries++;
            }
        }

        return new JobbaMassTransitJobCancelRequestResult<TJob> { JobId = jobId, WasCancelled = false };
    }
}
