using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        _dbContext = _testContext.CreateContext();
        _jobRegistration = AddRegistration();
        _fixture.Inject(_dbContext);
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

    private void AddJobs(List<JobEntity> jobEntities)
    {
        _dbContext.Jobs.AddRange(jobEntities);
        _dbContext.SaveChanges();

        var jobs = _dbContext.Jobs.ToList();
        jobs.Count.Should().Be(jobEntities.Count);
    }

    private JobEntity CreateJob(JobStatus status, bool isOutOfRetry = false) =>
        _fixture.Build<JobEntity>()
            .With(x => x.JobRegistrationId, _jobRegistration.Id)
            .With(x => x.JobParameters, new TestModels.FooParams { Baz = "baz" })
            .With(x => x.JobState, new TestModels.FooState { Bar = "bar" })
            .With(x => x.Status, status)
            .With(x => x.IsOutOfRetry, isOutOfRetry)
            .Create();

    [TestMethod]
    public async Task Jobba_Ef_Job_List_Store_Should_Get_Active_Jobs()
    {
        //arrange
        AddJobs([
            CreateJob(JobStatus.Completed),
            CreateJob(JobStatus.Cancelled),
            CreateJob(JobStatus.InProgress),
            CreateJob(JobStatus.Enqueued),
            CreateJob(JobStatus.Faulted),
            CreateJob(JobStatus.ForceCancelled),
            CreateJob(JobStatus.Unknown)
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
        AddJobs([
            CreateJob(JobStatus.Completed), // Should NOT retry (completed)
            CreateJob(JobStatus.Cancelled), // Should NOT retry (cancelled)
            CreateJob(JobStatus.InProgress), // Should NOT retry (in progress)
            CreateJob(JobStatus.Enqueued), // Should NOT retry (enqueued)
            CreateJob(JobStatus.Faulted), // Should retry
            CreateJob(JobStatus.ForceCancelled), // Should retry
            CreateJob(JobStatus.Unknown), // Should retry
            CreateJob(JobStatus.Faulted, true), // Should NOT retry (out of retry)
        ]);

        var listStore = _fixture.Create<JobbaEfJobListStore>();

        //act
        var jobsToRetry = await listStore.GetJobsToRetry(default);

        //assert
        jobsToRetry.Count().Should().Be(3);
    }
}
