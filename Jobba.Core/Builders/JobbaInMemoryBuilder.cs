using Jobba.Core.Implementations.Repositories.InMemory;
using Jobba.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Core.Builders;

/// <summary>
/// Encapsulates logic for in memory versions of the repositories
/// </summary>
public class JobbaInMemoryBuilder
{
    /// <summary>
    /// The root jobba builder.
    /// </summary>
    public JobbaBuilder Builder { get; set; }

    public JobbaInMemoryBuilder(JobbaBuilder jobbaBuilder)
    {
        Builder = jobbaBuilder;

        jobbaBuilder.Services.TryAddScoped<IJobListStore, InMemoryJobListStore>();
        jobbaBuilder.Services.TryAddScoped<IJobProgressStore, InMemoryJobProgressStore>();
        jobbaBuilder.Services.TryAddScoped<IJobStore, InMemoryJobStore>();
        jobbaBuilder.Services.TryAddScoped<IJobCleanUpStore, InMemoryJobCleanUpStore>();
        jobbaBuilder.Services.TryAddScoped<IJobRegistrationStore, InMemoryJobRegistrationStore>();
    }
}
