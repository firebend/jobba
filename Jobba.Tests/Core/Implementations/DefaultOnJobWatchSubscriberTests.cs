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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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

        var watchEvent = new JobWatchEvent<TestModels.FooJob>(
            jobId,
            typeof(TestModels.FooParams).AssemblyQualifiedName,
            typeof(TestModels.FooState).AssemblyQualifiedName,
            jobRegistrationId);

        var watcher = fixture.Freeze<Mock<IJobWatcher<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>>();
        watcher.Setup(x => x.WatchJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new AutoMoqCustomization());
        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IJobWatcher<,,>).MakeGenericType(typeof(TestModels.FooJob), typeof(TestModels.FooParams), typeof(TestModels.FooState)), watcher.Object }
        }));

        var registry = new JobTypeRegistry();
        registry.RegisterJobType<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>();

        var scopeFactory = fixture.Create<IServiceScopeFactory>();
        var service = new DefaultOnJobWatchSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(
            NullLogger<DefaultOnJobWatchSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>.Instance,
            scopeFactory);

        //act
        await service.WatchJobAsync(watchEvent, default);

        //assert
        watcher.Verify(x => x.WatchJobAsync(
            It.Is<Guid>(guid => guid == jobId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
