using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfJobRegistrationStoreTests
{
    private Fixture _fixture;
    private EfTestContext _testContext;
    private JobbaDbContext _dbContext;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _testContext = new EfTestContext();
        _dbContext = _testContext.CreateContext(_fixture);

        _fixture.Freeze<Mock<IJobbaGuidGenerator>>().Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid);
        _fixture.Freeze<Mock<IJobSystemInfoProvider>>().Setup(x => x.GetSystemInfo())
            .Returns(TestModels.TestSystemInfo);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose();
        _testContext.Dispose();
    }

    [TestMethod]
    public async Task Should_Register_NewJob()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);

        //assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
    }

    [TestMethod]
    public async Task Should_Update_ExistingJob()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        registration.NextExecutionDate = DateTimeOffset.UtcNow.AddDays(1);
        registration.PreviousExecutionDate = DateTimeOffset.UtcNow.AddDays(-1);

        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);

        var duplicateRegistration = _fixture.JobCronRegistrationBuilder().Create();
        duplicateRegistration.DefaultParams = new TestModels.FooParams { Baz = "baz2" };

        var updated = await store.RegisterJobAsync(duplicateRegistration, default);

        //assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        updated.Should().NotBeNull();
        updated.Id.Should().Be(result.Id);
        (updated.DefaultParams as TestModels.FooParams)!.Baz.Should().Be("baz2");
        updated.NextExecutionDate.Should().Be(result.NextExecutionDate);
        updated.PreviousExecutionDate.Should().Be(result.PreviousExecutionDate);
    }

    [TestMethod]
    public async Task Should_Update_ExistingJob_And_ExecutionDates_When_CronExpressionChanged()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        registration.NextExecutionDate = DateTimeOffset.UtcNow.AddDays(1);
        registration.PreviousExecutionDate = DateTimeOffset.UtcNow.AddDays(-1);

        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);

        var duplicateRegistration = _fixture.JobCronRegistrationBuilder().Create();
        duplicateRegistration.CronExpression = "0 0 0 1 1 ? 2024";

        var updated = await store.RegisterJobAsync(duplicateRegistration, default);

        //assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        updated.Should().NotBeNull();
        updated.Id.Should().Be(result.Id);
        updated.NextExecutionDate.Should().BeNull();
        updated.PreviousExecutionDate.Should().BeNull();
    }

    [TestMethod]
    public async Task Should_Get_Job_Registration()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        jobRegistration.Should().NotBeNull();
        jobRegistration!.Id.Should().Be(result.Id);
    }

    [TestMethod]
    public async Task Should_Get_Job_Registration_By_JobName()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        var jobRegistration = await store.GetByJobNameAsync(registration.JobName, default);

        //assert
        jobRegistration.Should().NotBeNull();
        jobRegistration!.Id.Should().Be(result.Id);
    }

    [TestMethod]
    public async Task Should_Get_Jobs_With_Cron_Expressions()
    {
        //arrange
        var registrationWithCron = _fixture.JobCronRegistrationBuilder().Create();
        var registrationWithoutCron = _fixture.JobRegistrationBuilder().With(x => x.JobName, "TestJob2").Create();
        var registrationDiffMoniker = _fixture.JobCronRegistrationBuilder()
            .With(x => x.JobName, "TestJob2").With(x => x.SystemMoniker, "AnotherOne").Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var resultWithCron = await store.RegisterJobAsync(registrationWithCron, default);
        await store.RegisterJobAsync(registrationWithoutCron, default);
        await store.RegisterJobAsync(registrationDiffMoniker, default);
        var jobRegistrations = (await store.GetJobsWithCronExpressionsAsync(default)).ToList();

        //assert
        jobRegistrations.Should().NotBeNullOrEmpty();
        jobRegistrations.Should().HaveCount(1);
        jobRegistrations.First().Id.Should().Be(resultWithCron.Id);
    }

    [TestMethod]
    public async Task Should_Update_Next_And_Previous_Invocation_Dates()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();
        var next = DateTimeOffset.UtcNow.AddDays(1);
        var previous = DateTimeOffset.UtcNow.AddDays(-1);

        //act
        var result = await store.RegisterJobAsync(registration, default);
        await store.UpdateNextAndPreviousInvocationDatesAsync(result.Id, next, previous, default);
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        jobRegistration.Should().NotBeNull();
        jobRegistration!.NextExecutionDate.Should().Be(next);
        jobRegistration!.PreviousExecutionDate.Should().Be(previous);
    }

    [TestMethod]
    public async Task Should_Remove_Job_Registration()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        await store.RemoveByIdAsync(result.Id, default);
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        jobRegistration.Should().BeNull();
    }

    [TestMethod]
    public async Task Should_Set_Job_Registration_Inactive()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder().Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        await store.SetIsInactiveAsync(result.Id, true, default);
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        jobRegistration.Should().NotBeNull();
        jobRegistration!.IsInactive.Should().BeTrue();
    }

    [TestMethod]
    public async Task Should_Round_Trip_TimeZoneId()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder()
            .With(x => x.TimeZoneId, "America/Chicago")
            .Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        _dbContext.ChangeTracker.Clear();
        var column = await _dbContext.Database
            .SqlQuery<string>($"SELECT TimeZoneId AS Value FROM JobRegistrations WHERE JobName = {registration.JobName}")
            .SingleAsync();
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        column.Should().Be("America/Chicago");
        jobRegistration.Should().NotBeNull();
        jobRegistration!.TimeZoneId.Should().Be("America/Chicago");
        jobRegistration.TimeZoneInfo.Id.Should().Be("America/Chicago");
    }

    [TestMethod]
    public async Task Should_Default_TimeZoneId_To_Utc_When_Not_Set()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder()
            .Without(x => x.TimeZoneId)
            .Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        _dbContext.ChangeTracker.Clear();
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        registration.TimeZoneId.Should().Be("UTC");
        jobRegistration.Should().NotBeNull();
        jobRegistration!.TimeZoneId.Should().Be("UTC");
        jobRegistration.TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
    }

    [TestMethod]
    public async Task Should_Update_TimeZoneId_And_Reset_TimeZoneInfo_On_Reregister()
    {
        //arrange
        var registration = _fixture.JobCronRegistrationBuilder()
            .With(x => x.TimeZoneId, "America/Chicago")
            .Create();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();
        var original = await store.RegisterJobAsync(registration, default);
        original.TimeZoneInfo.Id.Should().Be("America/Chicago");

        var reRegistration = _fixture.JobCronRegistrationBuilder()
            .With(x => x.TimeZoneId, "Asia/Tokyo")
            .Create();

        //act
        var updated = await store.RegisterJobAsync(reRegistration, default);
        _dbContext.ChangeTracker.Clear();
        var jobRegistration = await store.GetJobRegistrationAsync(original.Id, default);

        //assert
        updated.Should().BeSameAs(original);
        updated.TimeZoneInfo.Id.Should().Be("Asia/Tokyo");
        jobRegistration.Should().NotBeNull();
        jobRegistration!.TimeZoneId.Should().Be("Asia/Tokyo");
        jobRegistration.TimeZoneInfo.Id.Should().Be("Asia/Tokyo");
    }
}
