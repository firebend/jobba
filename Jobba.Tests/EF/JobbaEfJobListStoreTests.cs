using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfJobListStoreTests
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
        _fixture.Freeze<Mock<IJobSystemInfoProvider>>().Setup(x => x.GetSystemInfo())
            .Returns(TestModels.TestSystemInfo);
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
        var jobRegistration = _fixture.JobRegistrationBuilder()
            .With(x => x.Id, Guid.NewGuid)
            .Create();
        _dbContext.JobRegistrations.Add(jobRegistration);
        _dbContext.SaveChanges();
        return jobRegistration;
    }

    private void AddJobs(List<JobEntity> jobEntities)
    {
        _dbContext.Jobs.AddRange(jobEntities);
        _dbContext.SaveChanges();

        var jobs = _dbContext.Jobs.ToList();
        jobs.Count.Should().Be(jobEntities.Count);
    }

    private JobEntity CreateJob(JobStatus status, bool isOutOfRetry = false) =>
        _fixture.JobBuilder(_jobRegistration.Id)
            .With(x => x.Status, status)
            .With(x => x.IsOutOfRetry, isOutOfRetry)
            .Create();

    [TestMethod]
    public async Task Jobba_Ef_Job_List_Store_Should_Get_Active_Jobs()
    {
        //arrange
        var jobWithDifferentMoniker = _fixture.JobBuilder(_jobRegistration.Id)
            .With(x => x.Status, JobStatus.InProgress)
            .With(x => x.SystemInfo, new JobSystemInfo("a", "b", "c", "d"))
            .Create();
        AddJobs([
            CreateJob(JobStatus.Completed),
            CreateJob(JobStatus.Cancelled),
            CreateJob(JobStatus.InProgress),
            CreateJob(JobStatus.Enqueued),
            CreateJob(JobStatus.Faulted),
            CreateJob(JobStatus.ForceCancelled),
            CreateJob(JobStatus.Unknown),
            jobWithDifferentMoniker
        ]);

        var listStore = _fixture.Create<JobbaEfJobListStore>();

        //act
        var activeJobs = await listStore.GetActiveJobs(default);

        //assert
        activeJobs.Count().Should().Be(2);
    }

    [TestMethod]
    public async Task Jobba_Ef_Job_List_Store_Should_Get_Jobs_To_Retry()
    {
        //arrange
        var jobWithDifferentMoniker = _fixture.JobBuilder(_jobRegistration.Id)
            .With(x => x.Status, JobStatus.Faulted)
            .With(x => x.IsOutOfRetry, false)
            .With(x => x.SystemInfo, new JobSystemInfo("a", "b", "c", "d"))
            .Create();
        AddJobs([
            CreateJob(JobStatus.Completed), // Should NOT retry (completed)
            CreateJob(JobStatus.Cancelled), // Should NOT retry (cancelled)
            CreateJob(JobStatus.InProgress), // Should NOT retry (in progress)
            CreateJob(JobStatus.Enqueued), // Should NOT retry (enqueued)
            CreateJob(JobStatus.Faulted), // Should retry
            CreateJob(JobStatus.ForceCancelled), // Should retry
            CreateJob(JobStatus.Unknown), // Should retry
            CreateJob(JobStatus.Faulted, true), // Should NOT retry (out of retry)
            jobWithDifferentMoniker
        ]);

        var listStore = _fixture.Create<JobbaEfJobListStore>();

        //act
        var jobsToRetry = await listStore.GetJobsToRetry(default);

        //assert
        jobsToRetry.Count().Should().Be(3);
    }
}
