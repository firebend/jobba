using System;
using Jobba.Core.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Builders;

public class JobbaEfBuilder
{
    public JobbaBuilder Builder { get; }

    public JobbaEfBuilder(JobbaBuilder jobbaBuilder, Action<IServiceProvider,DbContextOptionsBuilder> dbContextOptionsBuilder, bool usePooled)
    {
        Builder = jobbaBuilder;

        if (usePooled)
        {
            Builder.Services.AddPooledDbContextFactory<JobbaDbContext>(dbContextOptionsBuilder);
        }
        else
        {
            Builder.Services.AddDbContextFactory<JobbaDbContext>(dbContextOptionsBuilder);
        }
    }
}
