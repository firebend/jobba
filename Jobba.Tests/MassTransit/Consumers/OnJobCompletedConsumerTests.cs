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

namespace Jobba.Tests.MassTransit.Consumers
{
    [TestClass]
    public class OnJobCompletedConsumerTests
    {
        [TestMethod]
        public async Task On_Job_Completed_Consumer_Should_Consume()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var subscriberMock = fixture.Freeze<Mock<IOnJobCompletedSubscriber>>();
            subscriberMock.Setup(x => x.OnJobCompletedAsync(It.IsAny<JobCompletedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
            {
                {
                    typeof(IEnumerable<IOnJobCompletedSubscriber>), new[]
                    {
                        subscriberMock.Object
                    }
                }
            }));

            var consumer = fixture.Create<OnJobCompleteConsumer>();

            //act
            await consumer.Consume(new Mock<ConsumeContext<JobCompletedEvent>>().Object);

            //assert
            subscriberMock.Verify(x => x.OnJobCompletedAsync(It.IsAny<JobCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
