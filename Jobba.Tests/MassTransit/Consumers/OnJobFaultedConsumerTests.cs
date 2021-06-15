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
    public class OnJobFaultedConsumerTests
    {
        [TestMethod]
        public async Task On_Job_Faulted_Consumer_Should_Consume()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var subscriberMock = fixture.Freeze<Mock<IOnJobFaultedSubscriber>>();
            subscriberMock.Setup(x => x.OnJobFaultedAsync(It.IsAny<JobFaultedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
            {
                {
                    typeof(IEnumerable<IOnJobFaultedSubscriber>), new[]
                    {
                        subscriberMock.Object
                    }
                }
            }));

            var consumer = fixture.Create<OnJobFaultedConsumer>();

            //act
            await consumer.Consume(new Mock<ConsumeContext<JobFaultedEvent>>().Object);

            //assert
            subscriberMock.Verify(x => x.OnJobFaultedAsync(It.IsAny<JobFaultedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
