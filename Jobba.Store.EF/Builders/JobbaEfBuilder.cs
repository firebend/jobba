using Jobba.Core.Builders;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Store.EF.Implementations;
using Jobba.Store.EF.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Store.EF.Builders;

public class JobbaEfBuilder
{
    public JobbaBuilder Builder { get; }

    public JobbaEfBuilder(JobbaBuilder jobbaBuilder)
    {
        Builder = jobbaBuilder;

        RegisterJobbaRequiredStores(jobbaBuilder);
    }

    public JobbaEfBuilder WithDbInitializer() => WithDbInitializer<DefaultJobbaDbInitializer>();

    public JobbaEfBuilder WithDbInitializer<T>() where T : class, IJobbaDbInitializer
    {
        // Builder.Services.AddHostedService<T>();
        return this;
    }

    private static void RegisterJobbaRequiredStores(JobbaBuilder jobbaBuilder)
    {
        jobbaBuilder.Services.TryAddScoped<IJobListStore, JobbaEfJobListStore>();
        jobbaBuilder.Services.TryAddScoped<IJobProgressStore, JobbaEfJobProgressStore>();
        jobbaBuilder.Services.TryAddScoped<IJobStore, JobbaEfJobStore>();
        jobbaBuilder.Services.TryAddScoped<IJobCleanUpStore, JobbaEfCleanUpStore>();
        jobbaBuilder.Services.TryAddScoped<IJobRegistrationStore, JobbaEfJobRegistrationStore>();
    }
}
