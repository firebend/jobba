using Jobba.Core.Implementations.Repositories.InMemory;
using Jobba.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Core.Builders;

public class JobbaInMemoryBuilder
{
    public JobbaBuilder Builder { get; set; }
    public JobbaInMemoryBuilder(JobbaBuilder jobbaBuilder)
    {
        Builder = jobbaBuilder;

        jobbaBuilder.Services.TryAddScoped<IJobListStore, InMemoryJobListStore>();
        jobbaBuilder.Services.TryAddScoped<IJobProgressStore, InMemoryJobProgressStore>();
        jobbaBuilder.Services.TryAddScoped<IJobStore, InMemoryJobStore>();
        jobbaBuilder.Services.TryAddScoped<IJobCleanUpStore, InMemoryJobCleanUpStore>();
    }
}
