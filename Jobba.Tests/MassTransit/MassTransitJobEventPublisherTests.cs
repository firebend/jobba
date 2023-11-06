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
    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Progress_Messages()
    {
        var message = new JobProgressEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await PublishEventHelper<JobProgressEvent>(publisher => publisher.PublishJobProgressEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Started_Messages()
    {
        var message = new JobStartedEvent(Guid.NewGuid(), Guid.NewGuid());
        await PublishEventHelper<JobStartedEvent>(publisher => publisher.PublishJobStartedEvent(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Cancelled_Messages()
    {
        var message = new JobCancelledEvent(Guid.NewGuid(), Guid.NewGuid());
        await PublishEventHelper<JobCancelledEvent>(publisher => publisher.PublishJobCancelledEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Completed_Messages()
    {
        var message = new JobCompletedEvent(Guid.NewGuid(), Guid.NewGuid());
        await PublishEventHelper<JobCompletedEvent>(publisher => publisher.PublishJobCompletedEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Faulted_Messages()
    {
        var message = new JobFaultedEvent(Guid.NewGuid(), Guid.NewGuid());
        await PublishEventHelper<JobFaultedEvent>(publisher => publisher.PublishJobFaultedEventAsync(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Restarted_Messages()
    {
        var message = new JobRestartEvent();
        await PublishEventHelper<JobRestartEvent>(publisher => publisher.PublishJobRestartEvent(message, default));
    }

    [TestMethod]
    public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Watched_Messages()
    {
        var message = new JobWatchEvent();
        var delay = TimeSpan.FromSeconds(1);
        await PublishEventHelper<JobWatchEvent>(publisher => publisher.PublishWatchJobEventAsync(message, delay, default), delay);
    }

    private static async Task PublishEventHelper<TMessage>(Func<IJobEventPublisher, Task> pubCallback, TimeSpan? delay = null)
        where TMessage : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddMassTransitTestHarness(cfg => cfg.AddDelayedMessageScheduler());
        var builder = new JobbaBuilder(serviceCollection);
        builder.UsingMassTransit().UsingInMemory();
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
