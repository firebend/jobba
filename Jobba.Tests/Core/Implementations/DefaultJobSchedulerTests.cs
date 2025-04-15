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

namespace Jobba.Tests.Core.Implementations;

[TestClass]
public class DefaultJobSchedulerTests
{
    [TestMethod]
    public async Task Default_Job_Scheduler_Should_Schedule_Job()
    {
        //arrange
        var fixture = new Fixture();

        var job = fixture.Freeze<Mock<IJob<DefaultJobParams, DefaultJobState>>>();
        job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jobRunner = fixture.Freeze<Mock<IJobRunner>>();
        jobRunner.Setup(x => x.RunJobAsync(It.IsAny<IJob<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new AutoMoqCustomization());
        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IJob<DefaultJobParams, DefaultJobState>), job.Object },
            { typeof(IJobRunner), jobRunner.Object }
        }));

        var jobId = Guid.NewGuid();

        var registrationStore = fixture.Freeze<Mock<IJobRegistrationStore>>();
        registrationStore.Setup(x => x.GetByJobNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string jobName, CancellationToken _) => new JobRegistration
            {
                Id = Guid.NewGuid(),
                JobType = typeof(IJob<DefaultJobParams, DefaultJobState>),
                JobParamsType = typeof(object),
                JobStateType = typeof(object),
                SystemMoniker = "Test",
                JobName = jobName
            });

        var request = fixture.Create<JobRequest<DefaultJobParams, DefaultJobState>>();
        request.JobId = Guid.Empty;
        request.IsRestart = false;
        request.JobType = typeof(IJob<DefaultJobParams, DefaultJobState>);

        var guidGenerator = fixture.Freeze<Mock<IJobbaGuidGenerator>>();
        guidGenerator.Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId);

        var store = fixture.Freeze<Mock<IJobStore>>();
        store.Setup(x => x.AddJobAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<DefaultJobParams, DefaultJobState> { Id = jobId });

        store.Setup(x => x.SetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<JobStatus>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
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
        publisher.Verify(x => x.PublishJobStartedEvent(It.IsAny<JobStartedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        publisher.Verify(
            x => x.PublishWatchJobEventAsync(It.IsAny<JobWatchEvent>(), It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(
            x => x.AddJobAsync(It.IsAny<JobRequest<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()), Times.Once);

        store.Verify(
            x => x.SetJobStatusAsync(jobId, JobStatus.Enqueued, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

        jobRunner.Verify(
            x => x.RunJobAsync(It.IsAny<IJob<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Default_Job_Scheduler_Should_ReSchedule_Job()
    {
        //arrange
        var fixture = new Fixture();
        var jobId = Guid.NewGuid();
        fixture.Customize(new AutoMoqCustomization());

        var job = fixture.Freeze<Mock<IJob<DefaultJobParams, DefaultJobState>>>();
        job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var registrationStore = fixture.Freeze<Mock<IJobRegistrationStore>>();
        registrationStore.Setup(x => x.GetByJobNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string jobName, CancellationToken _) => new JobRegistration
            {
                Id = Guid.NewGuid(),
                JobType = typeof(IJob<DefaultJobParams, DefaultJobState>),
                JobParamsType = typeof(object),
                JobStateType = typeof(object),
                SystemMoniker = "Test",
                JobName = jobName
            });

        var jobRunner = fixture.Freeze<Mock<IJobRunner>>();
        jobRunner.Setup(x => x.RunJobAsync(It.IsAny<IJob<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Customize(new ServiceProviderCustomization(
            new Dictionary<Type, object>
            {
                { typeof(IJob<DefaultJobParams, DefaultJobState>), job.Object },
                { typeof(IJobRunner), jobRunner.Object }
            }));

        var request = fixture.Create<JobRequest<DefaultJobParams, DefaultJobState>>();
        request.JobId = jobId;
        request.IsRestart = true;
        request.JobType = typeof(IJob<DefaultJobParams, DefaultJobState>);
        request.NumberOfTries = 2;
        request.JobWatchInterval = TimeSpan.FromMinutes(1);

        var store = fixture.Freeze<Mock<IJobStore>>();
        store.Setup(x => x.SetJobAttempts<DefaultJobParams, DefaultJobState>(jobId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobInfo<DefaultJobParams, DefaultJobState>
            {
                Id = jobId,
                CurrentNumberOfTries = 2,
                MaxNumberOfTries = 5
            });

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

        store.Verify(x => x.SetJobAttempts<DefaultJobParams, DefaultJobState>(
            jobId,
            2,
            It.IsAny<CancellationToken>()), Times.Once);

        store.Verify(
            x => x.SetJobStatusAsync(jobId, JobStatus.Enqueued, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

        jobRunner.Verify(
            x => x.RunJobAsync(It.IsAny<IJob<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
