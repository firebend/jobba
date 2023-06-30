using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Implementations;
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

        var mocks = Enumerable
            .Range(1, 3)
            .Select(_ => fixture.Freeze<Mock<IJobbaMassTransitConsumer>>().Object)
            .ToList();

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IEnumerable<IJobbaMassTransitConsumer>), mocks } }));

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

        var consumerMocks = Enumerable
            .Range(1, 3)
            .Select(_ => fixture.Freeze<Mock<IJobbaMassTransitConsumer>>().Object)
            .ToList();

        var jobMocks = Enumerable
            .Range(1, 3)
            .Select(index =>
            {
                var jobMock = fixture.Freeze<Mock<IJob>>();
                jobMock.Setup(x => x.JobName).Returns($"Fake Job {index}");
                return jobMock.Object;
            })
            .ToList();

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IEnumerable<IJobbaMassTransitConsumer>), consumerMocks },
            { typeof(IEnumerable<IJob>), jobMocks }
        }));

        var service = fixture.Create<JobbaMassTransitConsumerInfoProvider>();

        //act
        var infos = service.GetConsumerInfos().ToList();

        //assert
        infos.Should().NotBeNullOrEmpty().And.Subject.Count().Should().Be(9);
        infos.Select(x => x.ConsumerType).Should().NotContainNulls();
        var queues = infos.Select(x => x.QueueName).ToList();
        queues.Should().NotContainNulls();
        queues.TrueForAll(x => x.StartsWith("Fake_Job_"));
    }
}
