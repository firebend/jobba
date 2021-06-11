using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Builders;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Extensions;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.MassTransit
{
    [TestClass]
    public class MassTransitJobEventPublisherTests
    {
        [TestMethod]
        public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Progress_Messages()
        {
            var message = new JobProgressEvent(Guid.NewGuid(), Guid.NewGuid());
            await PublishEventHelper<JobProgressEvent>(publisher => publisher.PublishJobProgressEventAsync(message, default));
        }

        [TestMethod]
        public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Started_Messages()
        {
            var message = new JobStartedEvent(Guid.NewGuid());
            await PublishEventHelper<JobStartedEvent>(publisher => publisher.PublishJobStartedEvent(message, default));
        }

        [TestMethod]
        public async Task MassTransit_Job_Event_Publisher_Should_Pub_Sub_Cancelled_Messages()
        {
            var message = new JobCancelledEvent(Guid.NewGuid());
            await PublishEventHelper<JobCancelledEvent>(publisher => publisher.PublishJobCancelledEventAsync(message, default));
        }

        private static async Task PublishEventHelper<TMessage>(Func<IJobEventPublisher, Task> pubCallback)
            where TMessage : class
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddMassTransitInMemoryTestHarness(cfg => cfg.AddMessageScheduler(new Uri("https://www.root.com")));
            var builder = new JobbaBuilder(serviceCollection);
            builder.UsingMassTransit();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var harness = serviceProvider.GetRequiredService<InMemoryTestHarness>();
            await harness.Start();


            try
            {
                var hostedService = (serviceProvider
                        .GetService<IEnumerable<IHostedService>>() ?? Array.Empty<IHostedService>())
                    .FirstOrDefault(x => x is MassTransitJobbaReceiverHostedService);

                hostedService.Should().NotBeNull();
                await hostedService.StartAsync(default);
                var publisher = serviceProvider.GetService<IJobEventPublisher>();
                publisher.Should().NotBeNull().And.BeOfType<MassTransitJobEventPublisher>();

                await pubCallback(publisher);
                (await harness.Published.Any<TMessage>()).Should().BeTrue();
                (await harness.Consumed.Any<TMessage>()).Should().BeTrue();
            }
            finally
            {
                await harness.Stop();

                await serviceProvider.DisposeAsync();
            }
        }
    }
}
