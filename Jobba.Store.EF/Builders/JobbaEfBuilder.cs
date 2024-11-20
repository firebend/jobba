using System;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Store.EF.Builders;

public class JobbaEfBuilder
{
    public JobbaBuilder Builder { get; }

    public JobbaEfBuilder(JobbaBuilder jobbaBuilder,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsBuilder, bool usePooled)
    {
        Builder = jobbaBuilder;


        RegisterJobbaRequiredStores(jobbaBuilder);
        RegisterEfRequiredServices(jobbaBuilder, dbContextOptionsBuilder, usePooled);
    }

    public void WithDbInitializer() => WithDbInitializer<DefaultJobbaDbInitializer>();

    public void WithDbInitializer<T>() where T : class, IJobbaDbInitializer => Builder.Services.TryAddScoped<IJobbaDbInitializer, T>();

    private static void RegisterJobbaRequiredStores(JobbaBuilder jobbaBuilder)
    {
        jobbaBuilder.Services.TryAddScoped<IJobListStore, JobbaEfJobListStore>();
        jobbaBuilder.Services.TryAddScoped<IJobProgressStore, JobbaEfJobProgressStore>();
        jobbaBuilder.Services.TryAddScoped<IJobStore, JobbaEfJobStore>();
        jobbaBuilder.Services.TryAddScoped<IJobCleanUpStore, JobbaEfCleanUpStore>();
        jobbaBuilder.Services.TryAddScoped<IJobRegistrationStore, JobbaEfJobRegistrationStore>();
    }

    private static void RegisterEfRequiredServices(JobbaBuilder jobbaBuilder,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsBuilder,
        bool usePooled)
    {
        if (usePooled)
        {
            jobbaBuilder.Services.AddPooledDbContextFactory<JobbaDbContext>(dbContextOptionsBuilder);
        }
        else
        {
            jobbaBuilder.Services.AddDbContextFactory<JobbaDbContext>(dbContextOptionsBuilder);
        }
    }
}
