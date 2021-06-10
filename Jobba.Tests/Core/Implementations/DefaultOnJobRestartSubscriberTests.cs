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

namespace Jobba.Tests.Core.Implementations
{
    [TestClass]
    public class DefaultOnJobRestartSubscriberTests
    {
        public class FooParams
        {
            public string Foo { get; set; }
        }

        public class FooState
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task Default_On_Job_Restart_Subscriber_Should_Restart_Jobs()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var restartEvent = new JobRestartEvent
            {
                JobId = Guid.NewGuid(),
                JobParamsTypeName = typeof(FooParams).AssemblyQualifiedName,
                JobStateTypeName = typeof(FooState).AssemblyQualifiedName
            };

            var lockMock = fixture.Freeze<Mock<IJobLockService>>();
            lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<IDisposable>().Object);

            var jobStoreMock = fixture.Freeze<Mock<IJobStore>>();
            jobStoreMock.Setup(x => x.GetJobByIdAsync<FooParams, FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<FooParams, FooState>
                {
                    CurrentNumberOfTries = 1,
                    MaxNumberOfTries = 5,
                    JobParameters = new FooParams { Foo = "fake params" },
                    CurrentState = new FooState { Foo = "fake state" },
                    JobWatchInterval = TimeSpan.FromMinutes(1),
                    JobType = typeof(object).AssemblyQualifiedName,
                    Status = JobStatus.Faulted,
                    Id = restartEvent.JobId
                });

            var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
            jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<FooParams, FooState>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<FooParams, FooState>());

            var subscriber = fixture.Create<DefaultOnJobRestartSubscriber>();

            //act
            await subscriber.OnJobRestartAsync(restartEvent, default);

            //assert
            lockMock.Verify(x => x.LockJobAsync(
                    It.Is<Guid>(jobId => jobId == restartEvent.JobId),
                    It.IsAny<CancellationToken>()), Times.Once);

            jobStoreMock.Verify(x => x.GetJobByIdAsync<FooParams, FooState>(
                It.Is<Guid>(jobId => jobId == restartEvent.JobId),
                It.IsAny<CancellationToken>()), Times.Once);

            jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
                It.Is<JobRequest<FooParams, FooState>>(jobRequest =>
                    jobRequest.IsRestart &&
                    jobRequest.JobId == restartEvent.JobId &&
                    jobRequest.JobParameters.Foo == "fake params" &&
                    jobRequest.InitialJobState.Foo == "fake state" &&
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
                JobParamsTypeName = typeof(FooParams).AssemblyQualifiedName,
                JobStateTypeName = typeof(FooState).AssemblyQualifiedName
            };

            var lockMock = fixture.Freeze<Mock<IJobLockService>>();
            lockMock.Setup(x => x.LockJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<IDisposable>().Object);

            var jobStoreMock = fixture.Freeze<Mock<IJobStore>>();
            jobStoreMock.Setup(x => x.GetJobByIdAsync<FooParams, FooState>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<FooParams, FooState>
                {
                    CurrentNumberOfTries = numberOfTries,
                    MaxNumberOfTries = 5,
                    JobParameters = new FooParams { Foo = "fake params" },
                    CurrentState = new FooState { Foo = "fake state" },
                    JobWatchInterval = TimeSpan.FromMinutes(1),
                    JobType = typeof(object).AssemblyQualifiedName,
                    Status = jobStatus,
                    Id = restartEvent.JobId
                });

            var jobSchedulerMock = fixture.Freeze<Mock<IJobScheduler>>();
            jobSchedulerMock.Setup(x => x.ScheduleJobAsync(It.IsAny<JobRequest<FooParams, FooState>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<FooParams, FooState>());

            var subscriber = fixture.Create<DefaultOnJobRestartSubscriber>();

            //act
            await subscriber.OnJobRestartAsync(restartEvent, default);

            //assert
            lockMock.Verify(x => x.LockJobAsync(
                    It.Is<Guid>(jobId => jobId == restartEvent.JobId),
                    It.IsAny<CancellationToken>()), Times.Once);

            jobStoreMock.Verify(x => x.GetJobByIdAsync<FooParams, FooState>(
                It.Is<Guid>(jobId => jobId == restartEvent.JobId),
                It.IsAny<CancellationToken>()), Times.Once);

            jobSchedulerMock.Verify(x => x.ScheduleJobAsync(
                It.IsAny<JobRequest<FooParams, FooState>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
