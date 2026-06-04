using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultJobReSchedulerTests
{
    [TestMethod]
    public async Task Default_Job_Re_Scheduler_Should_Publish_Event()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var jobIds = fixture.CreateMany<Guid>(5);
        var jobBases = jobIds.Select(x => new JobInfoBase
        {
            Id = x,
            JobTypeName = typeof(TestModels.FooJob).AssemblyQualifiedName,
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName
        }).ToArray();

        var mockDispatcher = fixture.Freeze<Mock<IJobEventDispatcher>>();

        var mockJobListStore = fixture.Freeze<Mock<IJobListStore>>();
        var mockJobStore = fixture.Freeze<Mock<IJobStore>>();
        var mockOptions = fixture.Freeze<Mock<IOptions<JobbaCoreOptions>>>();
        mockOptions.Setup(x => x.Value).Returns(new JobbaCoreOptions());

        var callOrder = new System.Collections.Generic.List<string>();
        mockJobListStore.Setup(x => x.GetJobsToRetry(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetJobsToRetry"))
            .ReturnsAsync(jobBases);
        mockJobStore.Setup(x => x.ReclaimOrphanedJobsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("ReclaimOrphanedJobs"))
            .ReturnsAsync(0);

        var rescheduler = fixture.Create<DefaultJobReScheduler>();

        //act
        await rescheduler.RestartFaultedJobsAsync(default);

        //assert
        mockDispatcher.Verify(
            x => x.PublishRestartAsync(It.IsAny<Type>(), It.IsAny<Guid>(), It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()), Times.Exactly(5));
        mockJobStore.Verify(x => x.ReclaimOrphanedJobsAsync(new JobbaCoreOptions().StaleMultiplier, It.IsAny<CancellationToken>()), Times.Once);

        callOrder[0].Should().Be("ReclaimOrphanedJobs");
        callOrder[1].Should().Be("GetJobsToRetry");
    }
}
