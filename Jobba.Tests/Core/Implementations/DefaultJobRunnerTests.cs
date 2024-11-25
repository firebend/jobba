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
public class DefaultJobRunnerTests
{
    private IFixture _fixture;
    private Mock<IJobStore> _store;
    private Mock<IJobEventPublisher> _publisher;
    private Mock<IJob<DefaultJobParams, DefaultJobState>> _job;
    private Mock<IJobCancellationTokenStore> _cancellationTokenStore;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _job = _fixture.Freeze<Mock<IJob<DefaultJobParams, DefaultJobState>>>();
        _job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cancellationTokenStore = _fixture.Freeze<Mock<IJobCancellationTokenStore>>();

        _cancellationTokenStore.Setup(x => x.CreateJobCancellationToken(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(new CancellationToken());

        _store = _fixture.Freeze<Mock<IJobStore>>();

        _store.Setup(x => x.SetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<JobStatus>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publisher = _fixture.Freeze<Mock<IJobEventPublisher>>();
    }

    [TestMethod]
    public async Task Default_Job_Scheduler_Should_Schedule_Job()
    {
        //arrange
        var context = _fixture.Create<JobStartContext<DefaultJobParams, DefaultJobState>>();
        context.IsRestart = false;

        //act
        var runner = _fixture.Create<DefaultJobRunner>();
        await runner.RunJobAsync(_job.Object, context, default);

        //assert
        _job.Verify(
            x => x.StartAsync(It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(
            x => x.SetJobStatusAsync(context.JobId, JobStatus.InProgress, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(
            x => x.SetJobStatusAsync(context.JobId, JobStatus.Completed, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _publisher.Verify(x => x.PublishJobCompletedEventAsync(
            It.Is<JobCompletedEvent>(jobCompletedEvent => jobCompletedEvent.JobId == context.JobId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Default_Job_Scheduler_Should_Handle_Force_Cancellation()
    {
        //arrange
        var context = _fixture.Create<JobStartContext<DefaultJobParams, DefaultJobState>>();
        context.IsRestart = true;
        context.CurrentNumberOfTries = 2;

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        //act
        var runner = _fixture.Create<DefaultJobRunner>();
        await runner.RunJobAsync(_job.Object, context, cancellationTokenSource.Token);

        //assert
        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.InProgress,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _job.Verify(x => x.StartAsync(
            It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.ForceCancelled,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Default_Job_Scheduler_Should_Handle_Cancellation()
    {
        //arrange
        var context = _fixture.Create<JobStartContext<DefaultJobParams, DefaultJobState>>();
        context.IsRestart = true;
        context.CurrentNumberOfTries = 2;

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        _cancellationTokenStore.Setup(x => x.CreateJobCancellationToken(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(cancellationTokenSource.Token);

        //act
        var runner = _fixture.Create<DefaultJobRunner>();
        await runner.RunJobAsync(_job.Object, context, default);

        //assert
        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.InProgress,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _job.Verify(x => x.StartAsync(
            It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.Cancelled,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Default_Job_Scheduler_Should_Handle_Job_Failure()
    {
        //arrange
        var context = _fixture.Create<JobStartContext<DefaultJobParams, DefaultJobState>>();
        _job.Setup(x => x.StartAsync(It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
                It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        //act
        var runner = _fixture.Create<DefaultJobRunner>();
        await runner.RunJobAsync(_job.Object, context, default);

        //assert
        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.InProgress,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _job.Verify(x => x.StartAsync(
            It.IsAny<JobStartContext<DefaultJobParams, DefaultJobState>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(x => x.LogFailureAsync(context.JobId,
            It.IsAny<Exception>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _publisher.Verify(x => x.PublishJobFaultedEventAsync(
            It.Is<JobFaultedEvent>(jobFaultedEvent => jobFaultedEvent.JobId == context.JobId),
            It.IsAny<CancellationToken>()), Times.Once);

        _store.Verify(x => x.SetJobStatusAsync(context.JobId,
            JobStatus.Cancelled,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
