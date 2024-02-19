using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Tests.AutoMoqCustomizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultOnJobWatchSubscriberTests
{
    [TestMethod]
    public async Task Default_On_Job_Watch_Subscriber_Tests()
    {
        //arrange
        var fixture = new Fixture();
        var jobId = Guid.NewGuid();
        var jobRegistrationId = Guid.NewGuid();

        var watchEvent = new JobWatchEvent(
            jobId,
            typeof(Foo).AssemblyQualifiedName,
            typeof(Foo).AssemblyQualifiedName,
            jobRegistrationId);

        var watcher = fixture.Freeze<Mock<IJobWatcher<Foo, Foo>>>();
        watcher.Setup(x => x.WatchJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new AutoMoqCustomization());
        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IJobWatcher<,>).MakeGenericType(typeof(Foo), typeof(Foo)), watcher.Object }
        }));

        var service = fixture.Create<DefaultOnJobWatchSubscriber>();

        //act
        await service.WatchJobAsync(watchEvent, default);

        //assert
        watcher.Verify(x => x.WatchJobAsync(
            It.Is<Guid>(guid => guid == jobId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    public class Foo : IJobParams, IJobState;
}
