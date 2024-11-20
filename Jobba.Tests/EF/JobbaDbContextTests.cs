using System;
using System.Linq;
using FluentAssertions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.DbContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaDbContextTests
{
    private SqliteConnection _connection;
    private JobbaDbContext _dbContext;

    [TestInitialize]
    public void TestSetup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new JobbaDbContext(new DbContextOptionsBuilder<JobbaDbContext>()
            .UseSqlite(_connection)
            // .LogTo(Console.WriteLine)
            .Options);
        _dbContext.Database.EnsureCreated();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _connection.Close();
        _dbContext.Dispose();
    }

    [TestMethod]
    public void Jobba_Db_Context_Should_Create_Context() =>
        _dbContext.Database.IsSqlite().Should().BeTrue();

    [TestMethod]
    public void Jobba_Db_Context_Should_Configure_Entities()
    {
        //arrange
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

        var jobEntity = new JobEntity
        {
            Id = Guid.NewGuid(),
            JobRegistrationId = jobRegistration.Id,
            JobName = "Test",
            JobType = "foo",
            JobParameters = new TestModels.FooParams { Baz = "baz" },
            JobParamsTypeName = typeof(TestModels.FooParams).AssemblyQualifiedName,
            JobState = new TestModels.FooState { Bar = "bar" },
            JobStateTypeName = typeof(TestModels.FooState).AssemblyQualifiedName,
            Status = JobStatus.InProgress
        };

        _dbContext.Jobs.Add(jobEntity);

        var jobProgress = new JobProgressEntity
        {
            Id = Guid.NewGuid(),
            JobId = jobEntity.Id,
            JobRegistrationId = jobRegistration.Id,
            JobState = new TestModels.FooState { Bar = "bah" },
            Progress = 0,
            Message = "test",
            Date = DateTimeOffset.UtcNow
        };

        _dbContext.JobProgress.Add(jobProgress);

        //act
        _dbContext.SaveChanges();

        //assert
        _dbContext.JobRegistrations.Count().Should().Be(1);
        _dbContext.Jobs.Count().Should().Be(1);
        _dbContext.JobProgress.Count().Should().Be(1);

        var registration = _dbContext.JobRegistrations.FirstOrDefault();
        registration.Should().NotBeNull();
        registration!.Id.Should().Be(jobRegistration.Id);
        registration.JobName.Should().Be(jobRegistration.JobName);
        registration.JobType.Should().Be(jobRegistration.JobType);
        registration.DefaultParams.Should().BeEquivalentTo(jobRegistration.DefaultParams);
        registration.DefaultState.Should().BeEquivalentTo(jobRegistration.DefaultState);


        var job = _dbContext.Jobs.FirstOrDefault();
        job.Should().NotBeNull();
        job!.Id.Should().Be(jobEntity.Id);
        job.JobName.Should().Be(jobEntity.JobName);
        job.JobType.Should().Be(jobEntity.JobType);
        job.JobParameters.Should().BeEquivalentTo(jobEntity.JobParameters);
        job.JobState.Should().BeEquivalentTo(jobEntity.JobState);

        var progress = _dbContext.JobProgress.FirstOrDefault();
        progress.Should().NotBeNull();
        progress!.Id.Should().Be(jobProgress.Id);
        progress.JobId.Should().Be(jobProgress.JobId);
        progress.JobRegistrationId.Should().Be(jobProgress.JobRegistrationId);
        progress.JobState.Should().BeEquivalentTo(jobProgress.JobState);
        progress.Progress.Should().Be(jobProgress.Progress);
        progress.Message.Should().Be(jobProgress.Message);
        progress.Date.Should().Be(jobProgress.Date);
    }
}
