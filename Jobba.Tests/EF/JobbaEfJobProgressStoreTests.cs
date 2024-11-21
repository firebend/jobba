using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfJobProgressStoreTests
{
    private Fixture _fixture;
    private EfTestContext _testContext;
    private JobbaDbContext _dbContext;
    private JobRegistration _jobRegistration;
    private JobEntity _job;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _testContext = new EfTestContext();
        _dbContext = _testContext.CreateContext(_fixture);
        _jobRegistration = AddRegistration();
        _job = AddJob();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose();
        _testContext.Dispose();
    }

    private JobRegistration AddRegistration()
    {
        var jobRegistration = JobRegistration.FromTypes<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(
            "Test",
            "Test",
            "0 0 0 1 1 ? 2099",
            new TestModels.FooParams { Baz = "baz" },
            new TestModels.FooState { Bar = "bar" },
            false,
            null);
        jobRegistration.Id = Guid.NewGuid();
        _dbContext.JobRegistrations.Add(jobRegistration);
        _dbContext.SaveChanges();
        return jobRegistration;
    }

    private JobEntity AddJob()
    {
        var job = _fixture.Build<JobEntity>()
            .With(x => x.JobRegistrationId, _jobRegistration.Id)
            .With(x => x.JobParameters, new TestModels.FooParams { Baz = "baz" })
            .With(x => x.JobState, new TestModels.FooState { Bar = "bar" })
            .With(x => x.Status, JobStatus.InProgress)
            .With(x => x.IsOutOfRetry, false)
            .Create();

        _dbContext.Jobs.Add(job);
        _dbContext.SaveChanges();

        return job;
    }

    private JobProgress<TestModels.FooState> CreateProgress() =>
        new JobProgress<TestModels.FooState>
        {
            JobId = _job.Id,
            JobRegistrationId = _job.JobRegistrationId,
            JobState = new TestModels.FooState { Bar = "bar" },
            Date = DateTimeOffset.UtcNow,
            Progress = (decimal)0.5
        };

    [TestMethod]
    public async Task Jobba_Ef_Job_Progress_Store_Should_Add_Progress()
    {
        //arrange
        var mockPublisher = _fixture.Freeze<Mock<IJobEventPublisher>>();
        mockPublisher.Setup(x => x.PublishJobProgressEventAsync(
                It.IsAny<JobProgressEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var progress = CreateProgress();

        var service = _fixture.Create<JobbaEfJobProgressStore>();

        //act
        await service.LogProgressAsync(progress, default);

        //assert
        mockPublisher.Verify(x => x.PublishJobProgressEventAsync(
                It.IsAny<JobProgressEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var updatedJob = await _dbContext.Jobs.FindAsync(_job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.JobState.Should().Be(progress.JobState);
        updatedJob.LastProgressDate.Should().Be(progress.Date);
        updatedJob.LastProgressPercentage.Should().Be(progress.Progress);

        var jobProgresses = await _dbContext.JobProgress.Where(x => x.JobId == _job.Id).ToListAsync();
        jobProgresses.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_Progress_Store_Should_Get_By_Id()
    {
        //arrange
        var progress = CreateProgress();
        var entity = JobProgressEntity.FromJobProgress(progress);
        entity.Id = Guid.NewGuid();
        _dbContext.JobProgress.Add(entity);
        await _dbContext.SaveChangesAsync();

        var service = _fixture.Create<JobbaEfJobProgressStore>();

        //act
        var savedProgress = await service.GetProgressById(entity.Id, default);

        //assert
        savedProgress.Should().NotBeNull();
        savedProgress!.Id.Should().Be(entity.Id);
    }
}
