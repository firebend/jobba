using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for generating a guid.
/// </summary>
public interface IJobbaGuidGenerator
{
    Task<Guid> GenerateGuidAsync(CancellationToken cancellationToken);
}
