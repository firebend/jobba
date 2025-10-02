using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for executing a job.
/// </summary>
public interface IJob
{
    public string JobName { get; }
}

/// <summary>
/// Encapsulates logic for executing a job with parameters and state
/// </summary>
/// <typeparam name="TJobParams">
/// The type of job parameters.
/// </typeparam>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public interface IJob<TJobParams, TJobState> : IJob
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    public Task StartAsync(JobStartContext<TJobParams, TJobState> jobStartContext, CancellationToken cancellationToken);
}
