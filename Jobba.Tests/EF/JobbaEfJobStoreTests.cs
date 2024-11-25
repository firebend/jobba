using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfJobStoreTests
{
    private Fixture _fixture;
    private EfTestContext _testContext;
    private JobbaDbContext _dbContext;
    private JobRegistration _jobRegistration;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _testContext = new EfTestContext();
        _dbContext = _testContext.CreateContext(_fixture);
        _jobRegistration = AddRegistration();
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

    [TestMethod]
    public async Task Jobba_Ef_Job_Store_Should_Add_Job()
    {
        //arrange
        var jobRequest = new JobRequest<TestModels.FooParams, TestModels.FooState>
        {
            Description = "test",
            JobName = _jobRegistration.JobName,
            JobWatchInterval = TimeSpan.FromSeconds(1),
            MaxNumberOfTries = 3,
            NumberOfTries = 0,
            JobParameters = new TestModels.FooParams { Baz = "baz" },
            InitialJobState = new TestModels.FooState { Bar = "bar" },
            JobType = typeof(TestModels.FooJob)
        };

        var systemInfo = new JobSystemInfo("a", "b", "c", "d");

        var registrationStore = _fixture.Freeze<Mock<IJobRegistrationStore>>();
        registrationStore.Setup(x => x.GetByJobNameAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_jobRegistration);

        var systemInfoProvider = _fixture.Freeze<Mock<IJobSystemInfoProvider>>();
        systemInfoProvider.Setup(x => x.GetSystemInfo())
            .Returns(systemInfo)
            .Verifiable();

        var service = _fixture.Create<JobbaEfJobStore>();

        //act
        var jobInfo = await service.AddJobAsync(jobRequest, default);

        //assert
        jobInfo.Should().NotBeNull();
        jobInfo.Id.Should().NotBeEmpty();
        jobInfo.Description.Should().BeEquivalentTo(jobRequest.Description);
        jobInfo.JobWatchInterval.Should().Be(jobRequest.JobWatchInterval);
        jobInfo.JobType.Should().Be(jobRequest.JobType.AssemblyQualifiedName);
        jobInfo.JobParameters.Should().NotBeNull();
        jobInfo.JobParameters.Should().BeOfType(typeof(TestModels.FooParams));
        jobInfo.CurrentState.Should().NotBeNull();
        jobInfo.CurrentState.Should().BeOfType(typeof(TestModels.FooState));
        jobInfo.MaxNumberOfTries.Should().Be(jobRequest.MaxNumberOfTries);
        jobInfo.CurrentNumberOfTries.Should().Be(jobRequest.NumberOfTries);

        systemInfoProvider.VerifyAll();

        var jobEntity = await _dbContext.Jobs.FindAsync(jobInfo.Id);
        jobEntity.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_Store_Should_Set_Attempts()
    {
        //arrange
        var job = AddJob();

        var service = _fixture.Create<JobbaEfJobStore>();

        //act
        var jobInfo =
            await service.SetJobAttempts<TestModels.FooParams, TestModels.FooState>(job.Id, 2, default);

        //assert
        jobInfo.Should().NotBeNull();
        jobInfo.CurrentNumberOfTries.Should().Be(2);
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_Store_Should_Set_Job_Status()
    {
        //arrange
        var job = AddJob();

        var service = _fixture.Create<JobbaEfJobStore>();
        var now = DateTimeOffset.UtcNow;

        //act
        await service.SetJobStatusAsync(job.Id, JobStatus.Completed, now, default);

        //assert
        var updatedJob = await _dbContext.Jobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(JobStatus.Completed);
        updatedJob!.LastProgressDate.Should().Be(now);
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_Store_Should_Log_Failure()
    {
        //arrange
        var job = AddJob();

        var service = _fixture.Create<JobbaEfJobStore>();
        var ex = new Exception("fake");

        //act
        await service.LogFailureAsync(job.Id, ex, default);

        //assert
        var updatedJob = await _dbContext.Jobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.FaultedReason.Should().Be(ex.ToString());
        updatedJob!.Status.Should().Be(JobStatus.Faulted);
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_Store_Should_Get_Job_Info_Base_By_Id()
    {
        //arrange
        var job = AddJob();
        var service = _fixture.Create<JobbaEfJobStore>();

        //act
        var jobInfoBase = await service.GetJobByIdAsync(job.Id, default);

        //assert
        jobInfoBase.Should().NotBeNull();
        jobInfoBase.Id.Should().Be(job.Id);
        jobInfoBase.Description.Should().Be(job.Description);
        jobInfoBase.Status.Should().Be(job.Status);
        jobInfoBase.FaultedReason.Should().Be(job.FaultedReason);
        jobInfoBase.EnqueuedTime.Should().Be(job.EnqueuedTime);
        jobInfoBase.JobType.Should().Be(job.JobType);
        jobInfoBase.JobWatchInterval.Should().Be(job.JobWatchInterval);
        jobInfoBase.LastProgressDate.Should().Be(job.LastProgressDate);
        jobInfoBase.CurrentNumberOfTries.Should().Be(job.CurrentNumberOfTries);
        jobInfoBase.MaxNumberOfTries.Should().Be(job.MaxNumberOfTries);
    }
}
