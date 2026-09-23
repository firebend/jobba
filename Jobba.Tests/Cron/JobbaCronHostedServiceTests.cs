using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Cron.HostedServices;
using Jobba.Cron.Interfaces;
using Jobba.Tests.AutoMoqCustomizations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Cron;

[TestClass]
public class JobbaCronHostedServiceTests
{
    [TestMethod]
    public async Task Jobba_Cron_Hosted_Service_Should_Swallow_Cancellation_Aggregate_Exception()
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var scheduler = fixture.Freeze<Mock<ICronScheduler>>();
        scheduler
            .Setup(x => x.EnqueueJobsAsync(It.IsAny<CronSchedulerContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new AggregateException(new OperationCanceledException())));

        var infoProvider = fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        infoProvider
            .Setup(x => x.GetSystemInfo())
            .Returns(new JobSystemInfo { SystemMoniker = "test" });

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(ICronScheduler), scheduler.Object },
            { typeof(IJobSystemInfoProvider), infoProvider.Object }
        }));

        var logger = new RecordingLogger();
        var service = new TestableJobbaCronHostedService(
            logger,
            fixture.Create<IServiceScopeFactory>());

        await service.RunAsync(default);

        scheduler.Verify(x => x.EnqueueJobsAsync(
            It.IsAny<CronSchedulerContext>(), It.IsAny<CancellationToken>()), Times.Once);
        logger.CriticalLogged.Should().BeFalse();
    }

    private sealed class RecordingLogger : ILogger<JobbaCronHostedService>
    {
        public bool CriticalLogged { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            CriticalLogged |= logLevel == LogLevel.Critical;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class TestableJobbaCronHostedService(
        ILogger<JobbaCronHostedService> logger,
        IServiceScopeFactory scopeFactory) : JobbaCronHostedService(logger, scopeFactory)
    {
        public Task RunAsync(CancellationToken stoppingToken)
            => DoWorkAsync(stoppingToken);

        protected override Task CenterTimerAsync(CancellationToken stoppingToken)
            => Task.CompletedTask;
    }
}
