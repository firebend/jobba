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
        _dbContext = _testContext.CreateContext();
        _fixture.Inject(_dbContext);

        _fixture.Freeze<Mock<IJobbaGuidGenerator>>().Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose();
        _testContext.Dispose();
    }

    private JobRegistration CreateRegistration(string name = "Test", string cron = "0 0 0 1 1 ? 2099")
    {
        var jobRegistration = JobRegistration.FromTypes<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>(
            name,
            name,
            cron,
            new TestModels.FooParams { Baz = "baz" },
            new TestModels.FooState { Bar = "bar" },
            false,
            null);
        return jobRegistration;
    }

    [TestMethod]
    public async Task Should_Register_NewJob()
    {
        //arrange
        var registration = CreateRegistration();
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
        var registration = CreateRegistration();
        registration.NextExecutionDate = DateTimeOffset.UtcNow.AddDays(1);
        registration.PreviousExecutionDate = DateTimeOffset.UtcNow.AddDays(-1);

        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);

        var duplicateRegistration = CreateRegistration();
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
        var registration = CreateRegistration();
        registration.NextExecutionDate = DateTimeOffset.UtcNow.AddDays(1);
        registration.PreviousExecutionDate = DateTimeOffset.UtcNow.AddDays(-1);

        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);

        var duplicateRegistration = CreateRegistration();
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
        var registration = CreateRegistration();
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
        var registration = CreateRegistration();
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
        var registrationWithCron = CreateRegistration();
        var registrationWithoutCron = CreateRegistration("Test2", string.Empty);
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var resultWithCron = await store.RegisterJobAsync(registrationWithCron, default);
        await store.RegisterJobAsync(registrationWithoutCron, default);
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
        var registration = CreateRegistration();
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
        var registration = CreateRegistration();
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
        var registration = CreateRegistration();
        var store = _fixture.Create<JobbaEfJobRegistrationStore>();

        //act
        var result = await store.RegisterJobAsync(registration, default);
        await store.SetIsInactiveAsync(result.Id, true, default);
        var jobRegistration = await store.GetJobRegistrationAsync(result.Id, default);

        //assert
        jobRegistration.Should().NotBeNull();
        jobRegistration!.IsInactive.Should().BeTrue();
    }
}
