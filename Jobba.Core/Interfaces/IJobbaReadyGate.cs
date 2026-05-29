using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Represents a gate that must be signaled before Jobba proceeds with startup tasks such as restarting faulted jobs.
/// Implementations can use this to ensure infrastructure (e.g. message bus consumers) is ready before events are published.
/// </summary>
/// <remarks>
/// <para>
/// Multiple gates can be registered; <see cref="JobbaHostedService"/> awaits all of them in parallel before performing
/// startup work that depends on infrastructure being ready.
/// </para>
/// <para>
/// Implementations should be thread-safe, idempotent (multiple callers may invoke <see cref="WaitAsync"/> concurrently),
/// and registered as singletons so the gate state outlives any particular service scope.
/// </para>
/// <para>
/// Implementations MUST honor the supplied <see cref="CancellationToken"/> and complete the returned task promptly when
/// the token is signaled (via <see cref="OperationCanceledException"/>).
/// </para>
/// </remarks>
public interface IJobbaReadyGate
{
    /// <summary>
    /// Waits until the gate is open (i.e. the underlying infrastructure is ready).
    /// </summary>
    /// <param name="cancellationToken">A token used to abandon the wait. Implementations must observe this token.</param>
    public Task WaitAsync(CancellationToken cancellationToken);
}
