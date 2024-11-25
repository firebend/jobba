#nullable enable
using System;
using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Sqlite.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static DbContextOptionsBuilder UsingSqlite(this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        Action<SqliteDbContextOptionsBuilder>? configureSqlite = null)
    {
        optionsBuilder.UseSqlite(
            connectionString,
            x =>
            {
                x.MigrationsAssembly(typeof(MigrationsContextFactory).Assembly.GetName().Name!);
                configureSqlite?.Invoke(x);
            });

        configureDbContext?.Invoke(optionsBuilder);

        return optionsBuilder;
    }

    public static JobbaBuilder UsingSqlite(this JobbaBuilder jobbaBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        Action<SqliteDbContextOptionsBuilder>? configureSqlite = null,
        Action<JobbaEfBuilder>? configureBuilder = null)
    {
        jobbaBuilder.Services.AddDbContext<JobbaDbContext>(
            options => options.UsingSqlite(connectionString, configureDbContext, configureSqlite), ServiceLifetime.Transient);

        var builder = new JobbaEfBuilder(jobbaBuilder);
        configureBuilder?.Invoke(builder);
        return jobbaBuilder;
    }
}
