using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations
{
    [TestClass]
    public class DefaultOnJobCancelSubscriberTests
    {
        [TestMethod]
        public async Task Default_on_Job_Cancel_Subscriber_Should_Have_Cancelled_Job()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var jobId = Guid.NewGuid();
            var cancelEvent = new CancelJobEvent(jobId);

            var mockCancellationTokenStore = fixture.Freeze<Mock<IJobCancellationTokenStore>>();
            mockCancellationTokenStore
                .Setup(x => x.CancelJob(It.IsAny<Guid>()))
                .Returns(true);

            var service = fixture.Create<DefaultOnJobCancelSubscriber>();

            //act
            var result = await service.CancelJobAsync(cancelEvent, default);

            //assert
            result.Should().BeTrue();
            mockCancellationTokenStore.Verify(x => x.CancelJob(It.Is<Guid>(guid => guid == jobId)), Times.Once);
        }

        [TestMethod]
        public async Task Default_on_Job_Cancel_Subscriber_Should_Not_Have_Cancelled_Job()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var jobId = Guid.NewGuid();
            var cancelEvent = new CancelJobEvent(jobId);

            var mockCancellationTokenStore = fixture.Freeze<Mock<IJobCancellationTokenStore>>();
            mockCancellationTokenStore
                .Setup(x => x.CancelJob(It.IsAny<Guid>()))
                .Returns(false);

            var service = fixture.Create<DefaultOnJobCancelSubscriber>();

            //act
            var result = await service.CancelJobAsync(cancelEvent, default);

            //assert
            result.Should().BeFalse();
            mockCancellationTokenStore.Verify(x => x.CancelJob(It.Is<Guid>(guid => guid == jobId)), Times.Once);
        }
    }
}
