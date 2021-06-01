﻿using System.IO;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Store.Mongo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobba.Sample
{
    internal static class Program
    {
        private static Task Main(string[] args) =>
            CreateHostBuilder(args).Build().RunAsync();

        private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddEnvironmentVariables();
                configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((_, _) =>
            {
            })
            .ConfigureServices((_, services) =>
            {
                services.AddJobba(jobba =>
                {
                    jobba.UsingMongo("mongodb://localhost:27017/jobba-sample", true);
                    jobba.AddJob<SampleJob, SampleJobParameters, SampleJobState>();
                });
                services.AddHostedService<SampleHostedService>();

            });
    }
}