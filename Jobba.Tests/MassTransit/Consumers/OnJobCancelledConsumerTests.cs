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
public class OnJobCancelledConsumerTests
{
    [TestMethod]
    public async Task On_Job_Cancelled_Consumer_Should_Consume()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var subscriberMock = fixture.Freeze<Mock<IOnJobCancelledSubscriber>>();
        subscriberMock.Setup(x => x.OnJobCancelledAsync(It.IsAny<JobCancelledEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            {
                typeof(IEnumerable<IOnJobCancelledSubscriber>), new[]
                {
                    subscriberMock.Object
                }
            }
        }));

        var consumer = fixture.Create<OnJobCancelledConsumer>();

        //act
        await consumer.Consume(new Mock<ConsumeContext<JobCancelledEvent>>().Object);

        //assert
        subscriberMock.Verify(x => x.OnJobCancelledAsync(It.IsAny<JobCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
