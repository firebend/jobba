using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Builders;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Extensions;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.MassTransit;

[TestClass]
public class MassTransitJobEventPublisherTests
{
    private static readonly Guid TestJobId = Guid.NewGuid();
    private static readonly Guid TestRegistrationId = Guid.NewGuid();

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Progress_Messages()
    {
        var message = new JobProgressEvent<TestModels.FooJob>(TestJobId, TestJobId, TestRegistrationId);
        await PublishEventHelper(message, publisher => publisher.PublishJobProgressEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Started_Messages()
    {
        var message = new JobStartedEvent<TestModels.FooJob>(TestJobId, TestRegistrationId);
        await PublishEventHelper(message, publisher => publisher.PublishJobStartedEvent(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Cancelled_Messages()
    {
        var message = new JobCancelledEvent<TestModels.FooJob>(TestJobId, TestRegistrationId);
        await PublishEventHelper(message, publisher => publisher.PublishJobCancelledEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Completed_Messages()
    {
        var message = new JobCompletedEvent<TestModels.FooJob>(TestJobId, TestRegistrationId);
        await PublishEventHelper(message, publisher => publisher.PublishJobCompletedEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Faulted_Messages()
    {
        var message = new JobFaultedEvent<TestModels.FooJob>(TestJobId, TestRegistrationId);
        await PublishEventHelper(message, publisher => publisher.PublishJobFaultedEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Restarted_Messages()
    {
        var message = new JobRestartEvent<TestModels.FooJob>();
        await PublishEventHelper(message, publisher => publisher.PublishJobRestartEvent<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Watched_Messages()
    {
        var message = new JobWatchEvent<TestModels.FooJob>();
        var delay = TimeSpan.FromSeconds(1);
        await PublishEventHelper(message, publisher => publisher.PublishWatchJobEventAsync<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(message, delay, default), delay);
    }

    private static async Task PublishEventHelper<TMessage>(TMessage message, Func<IJobEventPublisher, Task> pubCallback, TimeSpan? delay = null)
        where TMessage : class, IJobbaEvent
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddMassTransitTestHarness(cfg => cfg.AddDelayedMessageScheduler());
        var builder = new JobbaBuilder(serviceCollection, "fake");
        builder.UsingMassTransit().UsingInMemory();
        builder.AddJob<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>("FooJob");
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var hostedService = (serviceProvider
                    .GetService<IEnumerable<IHostedService>>() ?? Array.Empty<IHostedService>())
                .FirstOrDefault(x => x is MassTransitJobbaReceiverHostedService);

            hostedService.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException
            await hostedService.StartAsync(default);
            var publisher = serviceProvider.GetService<IJobEventPublisher>();
            publisher.Should().NotBeNull().And.BeOfType<MassTransitJobEventPublisher>();

            await pubCallback(publisher);
            message.SystemMoniker.Should().Be("fake");

            if (delay.HasValue)
            {
                await Task.Delay(delay.Value);
            }
            else
            {
                // the harness doesn't count scheduled messages as published.
                (await harness.Published.Any<TMessage>()).Should().BeTrue();
            }

            (await harness.Consumed.Any<TMessage>()).Should().BeTrue();
        }
        finally
        {
            await serviceProvider.DisposeAsync();
        }
    }
}
