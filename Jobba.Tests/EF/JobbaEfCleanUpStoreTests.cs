using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfCleanUpStoreTests
{
    private Fixture _fixture;
    private EfTestContext _testContext;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());

        _testContext = new EfTestContext();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _testContext.Dispose();
    }

    [TestMethod]
    public async Task Jobba_Ef_Clean_Up_Store_Should_Remove_Completed_Jobs()
    {
        //arrange
        await using var dbContext = _testContext.CreateContext(_fixture);

        var jobRegistration = JobRegistration.FromTypes<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(
            "Test",
            "Test",
            "0 0 0 1 1 ? 2099",
            new TestModels.FooParams { Baz = "baz" },
            new TestModels.FooState { Bar = "bar" },
            false,
            null);
        jobRegistration.Id = Guid.NewGuid();
        dbContext.JobRegistrations.Add(jobRegistration);

        var jobEntities = _fixture.Build<JobEntity>()
            .With(x => x.JobRegistrationId, jobRegistration.Id)
            .With(x => x.JobParameters, new TestModels.FooParams { Baz = "baz" })
            .With(x => x.JobState, new TestModels.FooState { Bar = "bar" })
            .CreateMany(5)
            .ToList();

        // Should clean up
        jobEntities[0].Status = JobStatus.Completed;
        jobEntities[0].LastProgressDate = DateTimeOffset.UtcNow.AddDays(-6);

        // Should clean up
        jobEntities[1].Status = JobStatus.Completed;
        jobEntities[1].LastProgressDate = DateTimeOffset.UtcNow.AddDays(-5);

        // Should NOT clean up (completed but not old enough)
        jobEntities[2].Status = JobStatus.Completed;
        jobEntities[2].LastProgressDate = DateTimeOffset.UtcNow.AddDays(-4);

        // Should NOT clean up (in progress)
        jobEntities[3].Status = JobStatus.InProgress;
        jobEntities[3].LastProgressDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Should NOT clean up (enqueued)
        jobEntities[4].Status = JobStatus.Enqueued;
        jobEntities[4].LastProgressDate = DateTimeOffset.UtcNow;

        dbContext.Jobs.AddRange(jobEntities);
        await dbContext.SaveChangesAsync();

        var jobs = await dbContext.Jobs.ToListAsync();
        jobs.Count.Should().Be(5);

        var sut = _fixture.Create<JobbaEfCleanUpStore>();

        //act
        await sut.CleanUpJobsAsync(TimeSpan.FromDays(5), default);

        //assert

        var jobsAfter = await dbContext.Jobs.ToListAsync();
        jobsAfter.Count.Should().Be(3);

        jobsAfter.Any(x => x.Id == jobEntities[0].Id).Should().BeFalse();
        jobsAfter.Any(x => x.Id == jobEntities[1].Id).Should().BeFalse();
    }
}
