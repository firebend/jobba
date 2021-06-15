using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Implementations.Consumers;
using Jobba.MassTransit.Models;
using Jobba.Tests.AutoMoqCustomizations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.MassTransit.Consumers
{
    [TestClass]
    public class OnJobCancelConsumerTests
    {
        [TestMethod]
        public async Task On_Job_Cancel_Consumer_Should_Consume()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var subscriberMock = fixture.Freeze<Mock<IOnJobCancelSubscriber>>();
            subscriberMock.Setup(x => x.OnJobCancellationRequestAsync(It.IsAny<CancelJobEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
            {
                {
                    typeof(IEnumerable<IOnJobCancelSubscriber>), new[]
                    {
                        subscriberMock.Object
                    }
                }
            }));

            var consumeContextMock = new Mock<ConsumeContext<CancelJobEvent>>();
            consumeContextMock.Setup(x => x.RespondAsync(It.IsAny<JobbaMassTransitJobCancelRequestResult>()))
                .Returns(Task.CompletedTask);

            var jobId = Guid.NewGuid();

            consumeContextMock.Setup(x => x.Message)
                .Returns(new CancelJobEvent(jobId));

            var consumer = fixture.Create<OnJobCancelConsumer>();

            //act
            await consumer.Consume(consumeContextMock.Object);

            //assert
            subscriberMock.Verify(x => x.OnJobCancellationRequestAsync(It.IsAny<CancelJobEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
