using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

public record JobOrchestrationResult(JobRegistration Registration, JobInfoBase JobInfo);

public record JobOrchestrationRequest<TJob, TParams, TState>(string JobName,
    string Description,
    string Cron = null)
    where TParams : IJobParams
    where TState : IJobState
    where TJob : IJob<TParams, TState>;

public interface IJobOrchestrationService
{
    Task<JobOrchestrationResult> OrchestrateJobAsync<TJob, TParams, TState>(
        JobOrchestrationRequest<TJob, TParams, TState> request,
        CancellationToken cancellationToken)
        where TParams : IJobParams
        where TState : IJobState
        where TJob : IJob<TParams, TState>;
}
