using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Tests.AutoMoqCustomizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations
{
    [TestClass]
    public class DefaultJobSchedulerTests
    {
        [TestMethod]
        public async Task Default_Job_Scheduler_Should_Schedule_Job()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> {{typeof(IJob<object, object>), job.Object}}));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = Guid.Empty;
            request.IsRestart = false;
            request.JobType = typeof(IJob<object, object>);

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.AddJobAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> {Id = Guid.NewGuid()});

            var publisher = fixture.Freeze<Mock<IJobEventPublisher>>();
            publisher.Setup(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            publisher.Setup(x => x.PublishWatchJobEventAsync(
                    It.IsAny<JobWatchEvent>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            //act
            var scheduler = fixture.Create<DefaultJobScheduler>();
            var jobInfo = await scheduler.ScheduleJobAsync(request, default);

            //assert
            jobInfo.Should().NotBeNull();
            publisher.Verify(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            publisher.Verify(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.AddJobAsync(It.IsAny<JobRequest<object, object>>(), It.IsAny<CancellationToken>()), Times.Once);
            job.Verify(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Default_Job_Scheduler_Should_ReSchedule_Job()
        {
            //arrange
            var fixture = new Fixture();
            var jobId = Guid.NewGuid();
            fixture.Customize(new AutoMoqCustomization());

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> {{typeof(IJob<object, object>), job.Object}}));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = jobId;
            request.IsRestart = true;
            request.JobType = typeof(IJob<object, object>);
            request.NumberOfTries = 2;

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.SetJobAttempts<object, object>(jobId, 2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> {Id = jobId, CurrentNumberOfTries = 2, MaxNumberOfTries = 5});

            var publisher = fixture.Freeze<Mock<IJobEventPublisher>>();
            publisher.Setup(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            publisher.Setup(x => x.PublishWatchJobEventAsync(
                    It.IsAny<JobWatchEvent>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            //act
            var scheduler = fixture.Create<DefaultJobScheduler>();
            var jobInfo = await scheduler.ScheduleJobAsync(request, default);

            //assert
            jobInfo.Should().NotBeNull();
            publisher.Verify(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            publisher.Verify(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobAttempts<object, object>(jobId, 2, It.IsAny<CancellationToken>()), Times.Once);
            job.Verify(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
