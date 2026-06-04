using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Jobba.Tests.AutoMoqCustomizations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.MassTransit;

[TestClass]
public class AbstractJobbaMassTransitConsumerTests
{
    [TestMethod]
    public async Task Abstract_Jobba_MassTransit_Consumer_Should_Consume()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var subscriberMock = fixture.Freeze<Mock<IOnJobProgressSubscriber<TestModels.FooJob>>>();
        subscriberMock.Setup(x => x.OnJobProgressAsync(It.IsAny<JobProgressEvent<TestModels.FooJob>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            {
                typeof(IEnumerable<IOnJobProgressSubscriber<TestModels.FooJob>>), new[]
                {
                    subscriberMock.Object
                }
            }
        }));

        var consumer = fixture.Create<FakeConsumer>();

        //act
        await consumer.Consume(new Mock<ConsumeContext<JobProgressEvent<TestModels.FooJob>>>().Object);

        //assert
        subscriberMock.Verify(x => x.OnJobProgressAsync(It.IsAny<JobProgressEvent<TestModels.FooJob>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class FakeConsumer : AbstractJobbaMassTransitConsumer<JobProgressEvent<TestModels.FooJob>, IOnJobProgressSubscriber<TestModels.FooJob>>
    {

        protected override Task HandleMessageAsync(IOnJobProgressSubscriber<TestModels.FooJob> subscriber, JobProgressEvent<TestModels.FooJob> message, CancellationToken cancellationToken) =>
            subscriber.OnJobProgressAsync(message, cancellationToken);

        public FakeConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }
    }
}
