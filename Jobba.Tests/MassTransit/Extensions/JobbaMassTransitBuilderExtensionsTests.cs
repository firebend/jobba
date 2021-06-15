using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Builders;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Extensions;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.MassTransit.Extensions
{
    [TestClass]
    public class JobbaMassTransitBuilderExtensionsTests
    {
        [TestMethod]
        public async Task Jobba_MassTransit_Builder_Extensions_Should_Register_Services()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMassTransitInMemoryTestHarness(cfg => cfg.AddDelayedMessageScheduler());
            var builder = new JobbaBuilder(serviceCollection);
            builder.UsingMassTransit();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var harness = serviceProvider.GetRequiredService<InMemoryTestHarness>();
            await harness.Start();

            try
            {
                serviceProvider.GetService<IRequestClient<CancelJobEvent>>().Should().NotBeNull();
                serviceProvider.GetService<IJobbaMassTransitConsumerInfoProvider>().Should().NotBeNull();
                serviceProvider.GetService<IJobEventPublisher>().Should().NotBeNull().And.BeOfType<MassTransitJobEventPublisher>();
                serviceProvider.GetService<JobbaMassTransitConfigurationContext>().Should().NotBeNull();
            }
            finally
            {
                await harness.Stop();

                await serviceProvider.DisposeAsync();
            }
        }
    }
}
