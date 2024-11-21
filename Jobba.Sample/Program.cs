using System;
using System.IO;
using System.Threading.Tasks;
using Jobba.Core.Builders;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Cron.Extensions;
using Jobba.MassTransit.Extensions;
using Jobba.Redis;
using Jobba.Store.EF.Sql.Extensions;
using Jobba.Store.EF.Sqlite.Extensions;
using Jobba.Store.Mongo;
using Jobba.Store.Mongo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Jobba.Sample;

internal static class Program
{
    private const string SerilogTemplate =
        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";

    private static async Task Main(string[] args)
    {
        var app = CreateHostBuilder(args).Build();
        await app.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(configHost =>
        {
            configHost.SetBasePath(Directory.GetCurrentDirectory());
            configHost.AddEnvironmentVariables();
            configHost.AddCommandLine(args);
            configHost.AddJsonFile("appsettings.json");
        })
        .ConfigureServices((ctx, services) =>
        {
            services
                .AddLogging()
                .AddJobba("jobba-sample", jobba =>
                {
                    jobba.UsingMassTransit();
                    ResolveStore(ctx.Configuration, jobba);
                    jobba.UsingLitRedis("localhost:6379,defaultDatabase=0")
                        .UsingCron(cron =>
                            cron.AddCronJob<SampleCronJob, DefaultJobParams, DefaultJobState>("* * * * *",
                                SampleCronJob.Name))
                        .AddJob<SampleJob, SampleJobParameters, SampleJobState>(SampleJob.Name)
                        .AddJob<SampleJobCancel, DefaultJobParams, DefaultJobState>(SampleJobCancel.Name);
                })
                .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/")
                .AddHostedService<SampleHostedService>();
        })
        .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: SerilogTemplate, theme: AnsiConsoleTheme.Literate));

    private static void ResolveStore(IConfiguration config, JobbaBuilder jobba)
    {
        var provider = config.GetValue("provider", StoreProviders.SqlServer);
        Console.WriteLine("Using provider: " + provider);
        switch (provider)
        {
            case StoreProviders.InMemory:
                jobba.UsingInMemory();
                break;
            case StoreProviders.Sqlite:
                jobba.UsingSqlite(config.GetConnectionString("Sqlite")!, options =>
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }, jb => jb.WithDbInitializer());
                break;
            case StoreProviders.SqlServer:
                jobba.UsingSqlServer(config.GetConnectionString("SqlServer")!,
                    options =>
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }, jb => jb.WithDbInitializer());
                break;
            case StoreProviders.MongoDb:
                JobbaMongoDbConfigurator.Configure();
                jobba.UsingMongo(config.GetConnectionString("MongoDb"), false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
