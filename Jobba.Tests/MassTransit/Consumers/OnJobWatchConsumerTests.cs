using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Implementations.Consumers;
using Jobba.Tests.AutoMoqCustomizations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.MassTransit.Consumers;

[TestClass]
public class OnJobWatchConsumerTests
{
    [TestMethod]
    public async Task On_Job_Watch_Consumer_Should_Consume()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var subscriberMock = fixture.Freeze<Mock<IOnJobWatchSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>>();
        subscriberMock.Setup(x => x.WatchJobAsync(It.IsAny<JobWatchEvent<TestModels.FooJob>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            {
                typeof(IEnumerable<IOnJobWatchSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>), new[]
                {
                    subscriberMock.Object
                }
            }
        }));

        var consumer = fixture.Create<OnJobWatchConsumer<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>();

        //act
        await consumer.Consume(new Mock<ConsumeContext<JobWatchEvent<TestModels.FooJob>>>().Object);

        //assert
        subscriberMock.Verify(x => x.WatchJobAsync(It.IsAny<JobWatchEvent<TestModels.FooJob>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
