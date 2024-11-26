using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.Implementations;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaDbContextTests
{
    private EfTestContext _testContext;

    [TestInitialize]
    public void TestSetup()
    {
        _testContext = new EfTestContext();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _testContext.Dispose();
    }

    [TestMethod]
    public void Jobba_Db_Context_Should_Create_Context()
    {
        using var dbContext = _testContext.CreateContext();
        dbContext.Database.IsSqlite().Should().BeTrue();
    }

    [TestMethod]
    public void Jobba_Db_Context_Should_Configure_Entities()
    {
        //arrange
        using var dbContext = _testContext.CreateContext();
        var jobRegistration = JobRegistration.FromTypes<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(
            "Test",
            "Test",
            "Test",
            "0 0 0 1 1 ? 2099",
            new TestModels.FooParams { Baz = "baz" },
            new TestModels.FooState { Bar = "bar" },
            false,
            null);
        jobRegistration.Id = Guid.NewGuid();

        dbContext.JobRegistrations.Add(jobRegistration);

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

        dbContext.Jobs.Add(jobEntity);

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

        dbContext.JobProgress.Add(jobProgress);

        //act
        dbContext.SaveChanges();

        //assert
        dbContext.JobRegistrations.Count().Should().Be(1);
        dbContext.Jobs.Count().Should().Be(1);
        dbContext.JobProgress.Count().Should().Be(1);

        var registration = dbContext.JobRegistrations.FirstOrDefault();
        registration.Should().NotBeNull();
        registration!.Id.Should().Be(jobRegistration.Id);
        registration.JobName.Should().Be(jobRegistration.JobName);
        registration.JobType.Should().Be(jobRegistration.JobType);
        registration.DefaultParams.Should().BeEquivalentTo(jobRegistration.DefaultParams);
        registration.DefaultState.Should().BeEquivalentTo(jobRegistration.DefaultState);


        var job = dbContext.Jobs.FirstOrDefault();
        job.Should().NotBeNull();
        job!.Id.Should().Be(jobEntity.Id);
        job.JobName.Should().Be(jobEntity.JobName);
        job.JobType.Should().Be(jobEntity.JobType);
        job.JobParameters.Should().BeEquivalentTo(jobEntity.JobParameters);
        job.JobState.Should().BeEquivalentTo(jobEntity.JobState);

        var progress = dbContext.JobProgress.FirstOrDefault();
        progress.Should().NotBeNull();
        progress!.Id.Should().Be(jobProgress.Id);
        progress.JobId.Should().Be(jobProgress.JobId);
        progress.JobRegistrationId.Should().Be(jobProgress.JobRegistrationId);
        progress.JobState.Should().BeEquivalentTo(jobProgress.JobState);
        progress.Progress.Should().Be(jobProgress.Progress);
        progress.Message.Should().Be(jobProgress.Message);
        progress.Date.Should().Be(jobProgress.Date);

        dbContext.JobRegistrations.Remove(registration);

        dbContext.SaveChanges();

        dbContext.JobRegistrations.Count().Should().Be(0);
        dbContext.Jobs.Count().Should().Be(0);
        dbContext.JobProgress.Count().Should().Be(0);
    }

    [TestMethod]
    public async Task DefaultJobbaDbInitializer_Should_Only_Run_Migrations_Once()
    {
        //arrange
        var mockDbContext = new Mock<IJobbaDbContext>();
        mockDbContext.Setup(x => x.MigrateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(5));
        var mockLogger = new Mock<ILogger<DefaultJobbaDbInitializer>>();
        var initializer = new DefaultJobbaDbInitializer(mockLogger.Object);

        //act
        var tasks = new[]
        {
            initializer.InitializeAsync(mockDbContext.Object, default),
            initializer.InitializeAsync(mockDbContext.Object, default),
            initializer.InitializeAsync(mockDbContext.Object, default)
        };
        await Task.WhenAll(tasks);

        await initializer.InitializeAsync(mockDbContext.Object, default);

        //assert
        mockDbContext.Verify(x => x.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
