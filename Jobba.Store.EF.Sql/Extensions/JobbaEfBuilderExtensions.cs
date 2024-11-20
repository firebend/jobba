using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.SqlMigrations.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static JobbaBuilder UsingSqlServer(this JobbaBuilder jobbaBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null,
        Action<JobbaEfBuilder>? configure = null)
    {
        jobbaBuilder.Services.AddDbContextFactory<JobbaDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                x => x.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!)
            );
            configureOptions?.Invoke(options);
        }, ServiceLifetime.Transient);

        var builder = new JobbaEfBuilder(jobbaBuilder);
        configure?.Invoke(builder);
        return jobbaBuilder;
    }
}
