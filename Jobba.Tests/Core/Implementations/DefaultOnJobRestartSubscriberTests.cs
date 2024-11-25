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
public class DefaultOnJobRestartSubscriberTests
{
    [TestMethod]
    public async Task Default_On_Job_Restart_Subscriber_Should_Restart_Jobs()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var restartEvent = new JobRestartEvent
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName
        };

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IDisposable>());

        var jobStoreMock = fixture.Freeze<Mock<IJobStore>>();
        jobStoreMock.Setup(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                CurrentNumberOfTries = 1,
                MaxNumberOfTries = 5,
                JobParameters = new TestModels.FooParams { Baz = "fake params" },
                CurrentState = new TestModels.FooState { Bar = "fake state" },
                JobWatchInterval = TimeSpan.FromMinutes(1),
                JobType = typeof(object).AssemblyQualifiedName,
                Status = JobStatus.Faulted,
                Id = restartEvent.JobId
            });

        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
        jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>());

        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobStoreMock.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
            It.Is<JobRequest<TestModels.FooParams, TestModels.FooState>>(jobRequest =>
                jobRequest.IsRestart &&
                jobRequest.JobId == restartEvent.JobId &&
                jobRequest.JobParameters.Baz == "fake params" &&
                jobRequest.InitialJobState.Bar == "fake state" &&
                jobRequest.NumberOfTries == 2 &&
                jobRequest.MaxNumberOfTries == 5 &&
                jobRequest.JobWatchInterval == TimeSpan.FromMinutes(1)
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [DataRow(JobStatus.Completed, 1)]
    [DataRow(JobStatus.InProgress, 1)]
    [DataRow(JobStatus.Enqueued, 1)]
    [DataRow(JobStatus.Faulted, 5)]
    [TestMethod]
    public async Task Default_On_Job_Restart_Subscriber_Should_Not_Restart_Jobs(JobStatus jobStatus, int numberOfTries)
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var restartEvent = new JobRestartEvent
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName
        };

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IDisposable>());

        var jobStoreMock = fixture.Freeze<Mock<IJobStore>>();
        jobStoreMock.Setup(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>
            {
                CurrentNumberOfTries = numberOfTries,
                MaxNumberOfTries = 5,
                JobParameters = new TestModels.FooParams { Baz = "fake params" },
                CurrentState = new TestModels.FooState { Bar = "fake state" },
                JobWatchInterval = TimeSpan.FromMinutes(1),
                JobType = typeof(object).AssemblyQualifiedName,
                Status = jobStatus,
                Id = restartEvent.JobId
            });

        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
        jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>());

        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobStoreMock.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
            It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
