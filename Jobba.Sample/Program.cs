using System.IO;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Cron.Extensions;
using Jobba.MassTransit.Extensions;
using Jobba.Redis;
using Jobba.Store.EF.Extensions;
using Jobba.Store.Mongo;
using Jobba.Store.Mongo.Extensions;
using Microsoft.EntityFrameworkCore;
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

    private static Task Main(string[] args)
    {
        JobbaMongoDbConfigurator.Configure();
        return CreateHostBuilder(args).Build().RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(configHost =>
        {
            configHost.SetBasePath(Directory.GetCurrentDirectory());
            configHost.AddEnvironmentVariables();
            configHost.AddCommandLine(args);
            configHost.AddJsonFile("appsettings.json");
        })
        .ConfigureServices((_, services) =>
        {
            services
                .AddLogging()
                .AddJobba("jobba-sample", jobba =>
                    jobba.UsingMassTransit()
                        .UsingEf((_, opts) =>
                            opts.UseSqlServer(
                                "Data Source=.;Initial Catalog=jobba-sample;Persist Security Info=False;User ID=sa;Password=Password0#@!;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;Max Pool Size=200;"))
                        // .UsingMongo("mongodb://localhost:27017/jobba-sample", false)
                        .UsingLitRedis("localhost:6379,defaultDatabase=0")
                        .UsingCron(cron =>
                            cron.AddCronJob<SampleCronJob, DefaultJobParams, DefaultJobState>("* * * * *",
                                SampleCronJob.Name))
                        .AddJob<SampleJob, SampleJobParameters, SampleJobState>(SampleJob.Name)
                        .AddJob<SampleJobCancel, DefaultJobParams, DefaultJobState>(SampleJobCancel.Name)
                )
                .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/")
                .AddHostedService<SampleHostedService>()
                ;
        })
        .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: SerilogTemplate, theme: AnsiConsoleTheme.Literate));
}
