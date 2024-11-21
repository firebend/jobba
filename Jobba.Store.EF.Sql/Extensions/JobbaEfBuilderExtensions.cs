#nullable enable
using System;
using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Sql.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static DbContextOptionsBuilder UsingSqlServer(this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        optionsBuilder.UseSqlServer(
            connectionString,
            x => x.MigrationsAssembly(typeof(MigrationsContextFactory).Assembly.GetName().Name!)
        );

        configureOptions?.Invoke(optionsBuilder);

        return optionsBuilder;
    }

    public static JobbaBuilder UsingSqlServer(this JobbaBuilder jobbaBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null,
        Action<JobbaEfBuilder>? configure = null)
    {
        jobbaBuilder.Services.AddDbContext<JobbaDbContext>(
            options => options.UsingSqlServer(connectionString, configureOptions), ServiceLifetime.Transient);

        var builder = new JobbaEfBuilder(jobbaBuilder);
        configure?.Invoke(builder);
        return jobbaBuilder;
    }
}
