using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Cron.Interfaces;

public interface ICronScheduler
{
    Task EnqueueJobsAsync(IServiceScope scope, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
}
