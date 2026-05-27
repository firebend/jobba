using System;

namespace Jobba.Core.Models;

/// <summary>
/// Configuration options for Jobba Core.
/// </summary>
public class JobbaCoreOptions
{
    /// <summary>
    /// The faulted reason set when reclaiming orphaned InProgress jobs.
    /// </summary>
    public const string OrphanedJobFaultedReason = "Reclaimed orphaned InProgress";

    /// <summary>
    /// Default JobWatchInterval used when a request omits one (or supplies <see cref="TimeSpan.Zero"/>).
    /// </summary>
    public static readonly TimeSpan DefaultJobWatchInterval = TimeSpan.FromSeconds(10);

    private int _staleMultiplier = 3;

    /// <summary>
    /// The multiplier applied to JobWatchInterval to determine when an InProgress job is considered stale.
    /// If a job's LastHeartbeatTime is older than (JobWatchInterval × StaleMultiplier), it will be reclaimed.
    /// Default value is 3. Must be at least 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than 1.</exception>
    public int StaleMultiplier
    {
        get => _staleMultiplier;
        set => _staleMultiplier = value < 1
            ? throw new ArgumentOutOfRangeException(nameof(StaleMultiplier), "StaleMultiplier must be at least 1.")
            : value;
    }
}
