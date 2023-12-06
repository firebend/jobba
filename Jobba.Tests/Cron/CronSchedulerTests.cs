using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Abstractions;
using Jobba.Cron.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Cron;

[TestClass]
public class CronSchedulerTests
{
    [TestMethod]
    public async Task Cron_Scheduler_Should_Schedule_New_Job() => await WatchForTestJobToRunAsync();

    private static async Task<IHost> WatchForTestJobToRunAsync()
    {
        var builder = CreateHostBuilder();

        var host = builder.Build();
        await host.StartAsync();

        var start = Stopwatch.GetTimestamp();

        while (CronSchedulerTestsJob.HasRan is false)
        {
            await Task.Delay(100);

            if (Stopwatch.GetElapsedTime(start).TotalMinutes > 4)
            {
                Assert.Fail("Job did not run.");
            }
        }

        return host;
    }

    [TestMethod]
    public async Task Cron_Scheduler_Should_Handle_Cron_Change()
    {
        var host = await WatchForTestJobToRunAsync();

        var store = host.Services.GetService<IJobRegistrationStore>();
        var job = await store.GetByJobNameAsync(CronSchedulerTestsJob.Name, default);

        const string everySecondMinute = "*/2 * * * *";
        job.CronExpression = everySecondMinute;

        await store.RegisterJobAsync(job, default);

        var start = Stopwatch.GetTimestamp();

        while (CronSchedulerTestsJob.RunCounter < 2)
        {
            await Task.Delay(100);

            if (Stopwatch.GetElapsedTime(start).TotalMinutes > 8)
            {
                Assert.Fail("Job did not run again");
            }
        }
    }

    private static IHostBuilder CreateHostBuilder()
        => Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddJobba("test", j =>
            {
                j.UsingInMemory()
                    .UsingCron(cron =>
                        cron.AddCronJob<CronSchedulerTestsJob, DefaultJobParams, DefaultJobState>("* * * * *", CronSchedulerTestsJob.Name));
            }));

    public class CronSchedulerTestsJob : AbstractCronJobBaseClass<DefaultJobParams, DefaultJobState>
    {
        public const string Name = "test";
        public static bool HasRan;
        public static int RunCounter;
        public static object Lock = new();

        private readonly ILogger<CronSchedulerTestsJob> _logger;

        public CronSchedulerTestsJob(IJobProgressStore progressStore, ILogger<CronSchedulerTestsJob> logger) : base(progressStore)
        {
            _logger = logger;
        }

        public override string JobName => Name;

        protected override Task OnStartAsync(JobStartContext<DefaultJobParams, DefaultJobState> jobStartContext, CancellationToken cancellationToken)
        {
            lock (Lock)
            {
                RunCounter++;
                HasRan = true;
                _logger.LogInformation("Job has started {Date} {Now}", jobStartContext.StartTime, DateTimeOffset.UtcNow);
            }

            return Task.CompletedTask;
        }
    }
}
