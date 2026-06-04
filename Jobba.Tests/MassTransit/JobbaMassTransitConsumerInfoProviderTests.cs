using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Implementations.Consumers;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using Jobba.Tests.AutoMoqCustomizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.MassTransit;

[TestClass]
public class JobbaMassTransitConsumerInfoProviderTests
{
    [TestMethod]
    public void Jobba_MassTransit_Consumer_Info_Provider_Should_Get_Consumers()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        fixture.Inject(new JobbaMassTransitConfigurationContext { QueueMode = JobbaMassTransitQueueMode.OneQueue });

        var openRegistrations = new List<JobbaMassTransitOpenConsumerRegistration>
        {
            new() { OpenConsumerType = typeof(OnJobCompleteConsumer<,,>) },
            new() { OpenConsumerType = typeof(OnJobFaultedConsumer<,,>) },
            new() { OpenConsumerType = typeof(OnJobProgressConsumer<,,>) }
        };

        var registrations = new List<JobRegistration>
        {
            new() { JobName = "Fake Job 1", SystemMoniker = "test", JobType = typeof(TestModels.FooJob), JobParamsType = typeof(TestModels.FooParams), JobStateType = typeof(TestModels.FooState) }
        };

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IEnumerable<JobbaMassTransitOpenConsumerRegistration>), openRegistrations },
            { typeof(IEnumerable<JobRegistration>), registrations }
        }));

        var service = fixture.Create<JobbaMassTransitConsumerInfoProvider>();

        //act
        var infos = service.GetConsumerInfos().ToList();

        //assert
        infos.Should().NotBeNullOrEmpty().And.Subject.Count().Should().Be(3);
        infos.Select(x => x.ConsumerType).Should().NotContainNulls();
    }

    [TestMethod]
    public void Jobba_MassTransit_Consumer_Info_Provider_Should_Get_Consumers_One_Per_Job()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        fixture.Inject(new JobbaMassTransitConfigurationContext { QueueMode = JobbaMassTransitQueueMode.OnePerJob });

        var openRegistrations = new List<JobbaMassTransitOpenConsumerRegistration>
        {
            new() { OpenConsumerType = typeof(OnJobCompleteConsumer<,,>) },
            new() { OpenConsumerType = typeof(OnJobFaultedConsumer<,,>) },
            new() { OpenConsumerType = typeof(OnJobProgressConsumer<,,>) }
        };

        var registrations = Enumerable
            .Range(1, 3)
            .Select(index =>
                new JobRegistration
                {
                    JobName = $"Fake Job {index}",
                    SystemMoniker = "test",
                    JobType = typeof(TestModels.FooJob),
                    JobParamsType = typeof(TestModels.FooParams),
                    JobStateType = typeof(TestModels.FooState)
                })
            .ToList();

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IEnumerable<JobbaMassTransitOpenConsumerRegistration>), openRegistrations },
            { typeof(IEnumerable<JobRegistration>), registrations }
        }));

        var service = fixture.Create<JobbaMassTransitConsumerInfoProvider>();

        //act
        var infos = service.GetConsumerInfos().ToList();

        //assert
        infos.Should().NotBeNullOrEmpty().And.Subject.Count().Should().Be(9);
        infos.Select(x => x.ConsumerType).Should().NotContainNulls();
        var queues = infos.Select(x => x.QueueName).ToList();
        queues.Should().NotContainNulls();
        queues.TrueForAll(x => x.StartsWith("Fake_Job_")).Should().BeTrue();
    }
}
