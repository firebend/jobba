using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

public interface IJobbaGuidGenerator
{
    Task<Guid> GenerateGuidAsync(CancellationToken cancellationToken);
}
