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
    [DataRow(JobStatus.Faulted)]
    [DataRow(JobStatus.ForceCancelled)]
    [DataRow(JobStatus.Unknown)]
    [TestMethod]
    public async Task Default_On_Job_Restart_Subscriber_Should_Restart_Jobs(JobStatus jobStatus)
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var restartEvent = new JobRestartEvent<TestModels.FooJob>
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName,
            SystemMoniker = "test-system"
        };

        var systemInfoProvider = fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        systemInfoProvider.Setup(x => x.GetSystemInfo())
            .Returns(new JobSystemInfo("test-system", "machine", "user", "os"));

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
                JobTypeName = typeof(object).AssemblyQualifiedName,
                Status = jobStatus,
                Id = restartEvent.JobId
            });

        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
        jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>());

        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.Is<string>(s => s == "restart"),
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

        var restartEvent = new JobRestartEvent<TestModels.FooJob>
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName,
            SystemMoniker = "test-system"
        };

        var systemInfoProvider = fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        systemInfoProvider.Setup(x => x.GetSystemInfo())
            .Returns(new JobSystemInfo("test-system", "machine", "user", "os"));

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
                JobTypeName = typeof(object).AssemblyQualifiedName,
                Status = jobStatus,
                Id = restartEvent.JobId
            });

        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
        jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<TestModels.FooParams, TestModels.FooState>());

        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.Is<string>(s => s == "restart"),
            It.IsAny<CancellationToken>()), Times.Once);

        jobStoreMock.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
            It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Default_On_Job_Restart_Subscriber_Should_Ignore_Different_System_Moniker()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var restartEvent = new JobRestartEvent<TestModels.FooJob>
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName,
            SystemMoniker = "other-system"
        };

        var systemInfoProvider = fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        systemInfoProvider.Setup(x => x.GetSystemInfo())
            .Returns(new JobSystemInfo("test-system", "machine", "user", "os"));

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        var jobStoreMock = fixture.Freeze<Mock<IJobStore>>();
        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();

        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        jobStoreMock.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        jobSchedulerMock.Verify(x => x.ScheduleJobAsync(It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Default_On_Job_Restart_Subscriber_Should_Not_Schedule_When_Job_Type_Cannot_Be_Resolved()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var restartEvent = new JobRestartEvent<TestModels.FooJob>
        {
            JobId = Guid.NewGuid(),
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName,
            SystemMoniker = "test-system"
        };

        var systemInfoProvider = fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        systemInfoProvider.Setup(x => x.GetSystemInfo())
            .Returns(new JobSystemInfo("test-system", "machine", "user", "os"));

        var lockMock = fixture.Freeze<Mock<IJobLockService>>();
        lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
                JobTypeName = "Missing.Namespace.FooJob, Missing.Assembly",
                Status = JobStatus.Faulted,
                Id = restartEvent.JobId
            });

        var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
        var subscriber = fixture.Create<DefaultOnJobRestartSubscriber<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>>();

        //act
        await subscriber.OnJobRestartAsync(restartEvent, default);

        //assert
        lockMock.Verify(x => x.LockJobAsync(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.Is<string>(s => s == "restart"),
            It.IsAny<CancellationToken>()), Times.Once);

        jobStoreMock.Verify(x => x.GetJobByIdAsync<TestModels.FooParams, TestModels.FooState>(
            It.Is<Guid>(jobId => jobId == restartEvent.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
            It.IsAny<JobRequest<TestModels.FooParams, TestModels.FooState>>(),
            It.IsAny<CancellationToken>()), Times.Never);

    }
}
