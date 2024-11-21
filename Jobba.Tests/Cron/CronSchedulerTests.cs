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
using Jobba.Store.Mongo.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Cron;

[TestClass]
public class CronSchedulerTests
{
    [TestMethod]
    public Task Cron_Scheduler_Should_Schedule_New_Job() => WatchForTestJobToRunAsync();

    [TestMethod]
    public Task Cron_Scheduler_Should_Handle_Cron_Change() => JobChangeCronExpressionTest();

    //[TestMethod]
    public Task Cron_Scheduler_Should_Handle_Cron_Change_Mongo() => JobChangeCronExpressionTest(false);

    private static async Task<IHost> WatchForTestJobToRunAsync(bool useInMemory = true)
    {
        var builder = CreateHostBuilder(useInMemory);

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

    private static async Task JobChangeCronExpressionTest(bool useInMemory = true)
    {
        var host = await WatchForTestJobToRunAsync(useInMemory);

        var store = host.Services.CreateScope().ServiceProvider.GetService<IJobRegistrationStore>();
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

    private static IHostBuilder CreateHostBuilder(bool useInMemory = true)
        => Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddJobba("test", j =>
            {
                if (useInMemory)
                {
                    j.UsingInMemory();
                }
                else
                {
                    j.UsingMongo("mongodb://localhost:27017/jobba-unit-tests", true);
                }

                j.UsingCron(cron => cron.AddCronJob<CronSchedulerTestsJob, DefaultJobParams, DefaultJobState>("* * * * *", CronSchedulerTestsJob.Name));
            }));

    public class CronSchedulerTestsJob : AbstractCronJobBaseClass<DefaultJobParams, DefaultJobState>
    {

#pragma warning disable CA2211
#pragma warning disable IDE1006
        // ReSharper disable InconsistentNaming
        public static bool HasRan;
        public static int RunCounter;
        // ReSharper restore InconsistentNaming
        private static readonly object Lock = new();
#pragma warning restore CA2211
#pragma warning restore IDE1006

        public const string Name = "test";

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
