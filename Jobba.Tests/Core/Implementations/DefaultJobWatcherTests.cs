using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultJobWatcherTests
{
    [TestMethod]
    [DataRow(JobStatus.InProgress)]
    [DataRow(JobStatus.Enqueued)]
    public async Task Default_Job_Watcher_Should_Watch_Job(JobStatus jobStatus)
    {
        //arrange
        var jobId = Guid.NewGuid();
        var fixture = new Fixture();
        var timeSpan = TimeSpan.FromSeconds(10);
        fixture.Customize(new AutoMoqCustomization());

        var mockJobStore = fixture.Freeze<Mock<IJobStore>>();
        mockJobStore.Setup(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                JobWatchInterval = timeSpan,
                Id = jobId,
                Status = jobStatus
            });

        var mockPublisher = fixture.Freeze<Mock<IJobEventPublisher>>();
        mockPublisher.Setup(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var watcher = fixture.Create<DefaultJobWatcher<TestModels.FooParams, TestModels.FooState>>();

        //act
        await watcher.WatchJobAsync(jobId, default);

        //assert
        mockJobStore.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(jobId, It.IsAny<CancellationToken>()), Times.Once);

        mockPublisher.Verify(x => x.PublishWatchJobEventAsync(It.Is<JobWatchEvent>(e =>
                e.JobId == jobId &&
                e.ParamsTypeName == typeof(TestModels.FooParams).AssemblyQualifiedName &&
                e.StateTypeName == typeof(TestModels.FooState).AssemblyQualifiedName),
            It.Is<TimeSpan>(t => t == timeSpan),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Default_Job_Watcher_Should_Not_Watch_Job_When_Completed()
    {
        //arrange
        var jobId = Guid.NewGuid();
        var fixture = new Fixture();
        var timeSpan = TimeSpan.FromSeconds(10);
        fixture.Customize(new AutoMoqCustomization());

        var mockJobStore = fixture.Freeze<Mock<IJobStore>>();
        mockJobStore.Setup(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                JobWatchInterval = timeSpan,
                Id = jobId,
                Status = JobStatus.Completed
            });

        var mockPublisher = fixture.Freeze<Mock<IJobEventPublisher>>();
        mockPublisher.Setup(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var watcher = fixture.Create<DefaultJobWatcher<TestModels.FooParams, TestModels.FooState>>();

        //act
        await watcher.WatchJobAsync(jobId, default);

        //assert
        mockJobStore.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(jobId, It.IsAny<CancellationToken>()), Times.Once);
        mockPublisher.Verify(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.Is<TimeSpan>(t => t == timeSpan), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Default_Job_Watcher_Should_Restart_Job_When_Faulted()
    {
        //arrange
        var jobId = Guid.NewGuid();
        var fixture = new Fixture();
        var timeSpan = TimeSpan.FromSeconds(10);
        fixture.Customize(new AutoMoqCustomization());

        var mockJobStore = fixture.Freeze<Mock<IJobStore>>();
        mockJobStore.Setup(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                JobWatchInterval = timeSpan,
                Id = jobId,
                Status = JobStatus.Faulted,
                CurrentNumberOfTries = 1,
                MaxNumberOfTries = 3,
                JobType = typeof(object).AssemblyQualifiedName,
                Description = "Fake",
                CurrentState = new TestModels.FooState { Bar = "fake state" },
                JobParameters = new TestModels.FooParams { Baz = "fake params" }
            });

        var mockPublisher = fixture.Freeze<Mock<IJobEventPublisher>>();
        mockPublisher.Setup(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockJobScheduler = fixture.Freeze<Mock<IJobScheduler>>();
        mockJobScheduler.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                JobWatchInterval = timeSpan,
                Id = jobId,
                Status = JobStatus.Faulted,
                CurrentNumberOfTries = 2,
                MaxNumberOfTries = 3,
                JobType = typeof(object).FullName,
                Description = "Fake",
                CurrentState = new TestModels.FooState { Bar = "fake state" },
                JobParameters = new TestModels.FooParams { Baz = "fake params" }
            });

        var watcher = fixture.Create<DefaultJobWatcher<TestModels.FooParams, TestModels.FooState>>();

        //act
        await watcher.WatchJobAsync(jobId, default);

        //assert
        mockJobStore.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(jobId, It.IsAny<CancellationToken>()), Times.Once);
        mockPublisher.Verify(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.Is<TimeSpan>(t => t == timeSpan), It.IsAny<CancellationToken>()),
            Times.Never);
        mockJobScheduler.Verify(x => x.ScheduleJobAsync(
                It.Is<JobRequest<TestModels.FooParams, TestModels.FooState>>(request =>
                    request.JobId == jobId &&
                    request.IsRestart &&
                    request.JobType == typeof(object) &&
                    request.Description == "Fake" &&
                    request.JobWatchInterval == TimeSpan.FromSeconds(10) &&
                    request.NumberOfTries == 2 &&
                    request.JobParameters.Baz == "fake params" &&
                    request.InitialJobState.Bar == "fake state"
                ),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
