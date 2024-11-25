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
        Builder.Services.TryAddSingleton<IJobbaDbInitializer, T>();
        return this;
    }

    private static void RegisterJobbaRequiredStores(JobbaBuilder jobbaBuilder)
    {
        jobbaBuilder.Services.TryAddTransient<IJobListStore, JobbaEfJobListStore>();
        jobbaBuilder.Services.TryAddTransient<IJobProgressStore, JobbaEfJobProgressStore>();
        jobbaBuilder.Services.TryAddTransient<IJobStore, JobbaEfJobStore>();
        jobbaBuilder.Services.TryAddTransient<IJobCleanUpStore, JobbaEfCleanUpStore>();
        jobbaBuilder.Services.TryAddTransient<IJobRegistrationStore, JobbaEfJobRegistrationStore>();
        jobbaBuilder.Services.TryAddTransient<IDbContextProvider, DefaultDbContextProvider>();
    }
}
