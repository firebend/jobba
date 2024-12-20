#nullable enable
using System;
using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Sql.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static DbContextOptionsBuilder UsingSqlServer(this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        Action<SqlServerDbContextOptionsBuilder>? configureSqlServer = null)
    {
        optionsBuilder.UseSqlServer(
            connectionString,
            x =>
            {
                x.MigrationsAssembly(typeof(MigrationsContextFactory).Assembly.GetName().Name!);
                x.MigrationsHistoryTable("__EFMigrationsHistory", JobbaDbContext.JobbaSchema);
                configureSqlServer?.Invoke(x);
            });

        configureDbContext?.Invoke(optionsBuilder);

        return optionsBuilder;
    }

    public static JobbaBuilder UsingSqlServer(this JobbaBuilder jobbaBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        Action<SqlServerDbContextOptionsBuilder>? configureSqlServer = null,
        Action<JobbaEfBuilder>? configureBuilder = null)
    {
        jobbaBuilder.Services.AddDbContext<JobbaDbContext>(
            options => options.UsingSqlServer(connectionString, configureDbContext, configureSqlServer), ServiceLifetime.Transient);

        var builder = new JobbaEfBuilder(jobbaBuilder);
        configureBuilder?.Invoke(builder);
        return jobbaBuilder;
    }
}
