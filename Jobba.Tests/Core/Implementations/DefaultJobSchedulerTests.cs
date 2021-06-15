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

            var jobId = Guid.NewGuid();

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJob<object, object>), job.Object } }));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = Guid.Empty;
            request.IsRestart = false;
            request.JobType = typeof(IJob<object, object>);

            var guidGenerator = fixture.Freeze<Mock<IJobbaGuidGenerator>>();
            guidGenerator.Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobId);

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.AddJobAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> { Id = jobId });

            store.Setup(x => x.SetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<JobStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
            //adding a delay because this test keeps failing on the CI server but always passes locally. :shrug:
            await Task.Delay(TimeSpan.FromSeconds(5));
            jobInfo.Should().NotBeNull();
            publisher.Verify(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            publisher.Verify(x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.AddJobAsync(It.IsAny<JobRequest<object, object>>(), It.IsAny<CancellationToken>()), Times.Once);
            job.Verify(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.Enqueued, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.InProgress, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.Completed, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
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

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJob<object, object>), job.Object } }));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = jobId;
            request.IsRestart = true;
            request.JobType = typeof(IJob<object, object>);
            request.NumberOfTries = 2;
            request.JobWatchInterval = TimeSpan.FromMinutes(1);

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.SetJobAttempts<object, object>(jobId, 2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> { Id = jobId, CurrentNumberOfTries = 2, MaxNumberOfTries = 5 });

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
            publisher.Verify(x => x.PublishJobStartedEvent(
                It.Is<JobStartedEvent>(@event => @event.JobId == jobId),
                It.IsAny<CancellationToken>()), Times.Once);

            publisher.Verify(x => x.PublishWatchJobEventAsync(
                It.Is<JobWatchEvent>(jobWatchEvent => jobWatchEvent.JobId == jobId),
                It.Is<TimeSpan>(timeSpan => timeSpan == TimeSpan.FromMinutes(1)),
                It.IsAny<CancellationToken>()), Times.Once);

            publisher.Verify(x => x.PublishJobCompletedEventAsync(
                It.Is<JobCompletedEvent>(jobCompletedEvent => jobCompletedEvent.JobId == jobId),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobAttempts<object, object>(
                jobId,
                2,
                It.IsAny<CancellationToken>()), Times.Once);

            job.Verify(x => x.StartAsync(
                It.IsAny<JobStartContext<object, object>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.Enqueued,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.InProgress,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.Completed,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Default_Job_Scheduler_Should_Handle_Force_Cancellation()
        {
            //arrange
            var fixture = new Fixture();
            var jobId = Guid.NewGuid();
            fixture.Customize(new AutoMoqCustomization());

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns((JobStartContext<object, object> _, CancellationToken cancellationToken) => Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJob<object, object>), job.Object } }));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = jobId;
            request.IsRestart = true;
            request.JobType = typeof(IJob<object, object>);
            request.NumberOfTries = 2;

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.SetJobAttempts<object, object>(jobId, 2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> { Id = jobId, CurrentNumberOfTries = 2, MaxNumberOfTries = 5 });

            store.Setup(x => x.LogFailureAsync(It.IsAny<Guid>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
            var cancellationTokenSource = new CancellationTokenSource();
            var jobInfo = await scheduler.ScheduleJobAsync(request, cancellationTokenSource.Token);

            await Task.Delay(TimeSpan.FromSeconds(1));
            cancellationTokenSource.Cancel();

            //assert
            jobInfo.Should().NotBeNull();
            publisher.Verify(x => x.PublishJobStartedEvent(
                It.IsAny<JobStartedEvent>(),
                It.IsAny<CancellationToken>()), Times.Once);

            publisher.Verify(x => x.PublishWatchJobEventAsync(
                It.IsAny<JobWatchEvent>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobAttempts<object, object>(
                jobId,
                2,
                It.IsAny<CancellationToken>()), Times.Once);

            job.Verify(x => x.StartAsync(
                It.IsAny<JobStartContext<object, object>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.Enqueued,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.InProgress,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.ForceCancelled,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Default_Job_Scheduler_Should_Handle_Cancellation()
        {
            //arrange
            var fixture = new Fixture();
            var jobId = Guid.NewGuid();
            fixture.Customize(new AutoMoqCustomization());

            var jobCancellationStore = new DefaultJobCancellationTokenStore();

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns((JobStartContext<object, object> _, CancellationToken cancellationToken) => Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJob<object, object>), job.Object } }));


            fixture.Register<IJobCancellationTokenStore>(() => jobCancellationStore);

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = jobId;
            request.IsRestart = true;
            request.JobType = typeof(IJob<object, object>);
            request.NumberOfTries = 2;

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.SetJobAttempts<object, object>(jobId, 2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> { Id = jobId, CurrentNumberOfTries = 2, MaxNumberOfTries = 5 });

            store.Setup(x => x.LogFailureAsync(It.IsAny<Guid>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
            var cancellationTokenSource = new CancellationTokenSource();
            var jobInfo = await scheduler.ScheduleJobAsync(request, cancellationTokenSource.Token);

            await Task.Delay(TimeSpan.FromSeconds(1));
            jobCancellationStore.CancelJob(jobId);

            //assert
            jobInfo.Should().NotBeNull();
            publisher.Verify(x => x.PublishJobStartedEvent(
                It.IsAny<JobStartedEvent>(),
                It.IsAny<CancellationToken>()), Times.Once);

            publisher.Verify(x => x.PublishWatchJobEventAsync(
                It.IsAny<JobWatchEvent>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobAttempts<object, object>(
                jobId,
                2,
                It.IsAny<CancellationToken>()), Times.Once);

            job.Verify(x => x.StartAsync(
                It.IsAny<JobStartContext<object, object>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.Enqueued,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.InProgress,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            store.Verify(x => x.SetJobStatusAsync(jobId,
                JobStatus.Cancelled,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Default_Job_Scheduler_Should_Handle_Job_Failure()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var jobId = Guid.NewGuid();

            var job = fixture.Freeze<Mock<IJob<object, object>>>();
            job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<object, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJob<object, object>), job.Object } }));

            fixture.Register<IJobCancellationTokenStore>(() => new DefaultJobCancellationTokenStore());

            var request = fixture.Create<JobRequest<object, object>>();
            request.JobId = Guid.Empty;
            request.IsRestart = false;
            request.JobType = typeof(IJob<object, object>);

            var guidGenerator = fixture.Freeze<Mock<IJobbaGuidGenerator>>();
            guidGenerator.Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobId);

            var store = fixture.Freeze<Mock<IJobStore>>();
            store.Setup(x => x.AddJobAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobInfo<object, object> { Id = jobId });

            store.Setup(x => x.SetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<JobStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.Enqueued, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.InProgress, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.SetJobStatusAsync(jobId, JobStatus.Completed, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
