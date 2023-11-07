using System.IO;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.MassTransit.Extensions;
using Jobba.Redis;
using Jobba.Store.Mongo;
using Jobba.Store.Mongo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample;

internal static class Program
{
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
                .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
                .AddJobba(jobba =>
                    jobba.UsingMassTransit()
                        .UsingMongo("mongodb://localhost:27017/jobba-sample", false)
                        .UsingLitRedis("localhost:6379,defaultDatabase=0")
                        .AddJob<SampleJob, SampleJobParameters, SampleJobState>("sample-job")
                        .AddJob<SampleJobCancel, object, object>("sample-job-cancel")
                )
                .AddJobbaSampleMassTransit("rabbitmq://guest:guest@localhost/")
                .AddHostedService<SampleHostedService>();
        });
}
