using Jobba.Core.Builders;
using Jobba.Store.EF.Builders;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Store.EF.Sqlite.Extensions;

public static class JobbaEfBuilderExtensions
{
    public static JobbaBuilder UsingSqlite(this JobbaBuilder jobbaBuilder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null,
        Action<JobbaEfBuilder>? configure = null)
    {
        var assemblyName = typeof(Marker).Assembly.GetName().Name!;
        Console.WriteLine("Configuring sqlite in assembly: " + assemblyName);
        jobbaBuilder.Services.AddDbContext<JobbaDbContext>(options =>
        {
            Console.WriteLine("Configuring sqlite 2");
            options.UseSqlite(
                connectionString,
                x => x.MigrationsAssembly(assemblyName)
            );
            configureOptions?.Invoke(options);
        }, ServiceLifetime.Transient);

        var builder = new JobbaEfBuilder(jobbaBuilder);
        configure?.Invoke(builder);
        return jobbaBuilder;
    }
}
