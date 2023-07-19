using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neleus.LambdaCompare;

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoJobStoreTests
{
    [TestMethod]
    public async Task Jobba_Mongo_Job_Store_Should_Add_Job()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());
        var jobRequest = fixture.Create<JobRequest<Foo, Foo>>();
        var jobEntity = JobEntity.FromRequest(jobRequest);

        var repo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        repo.Setup(x => x.AddAsync(It.IsAny<JobEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobEntity);

        var service = fixture.Create<JobbaMongoJobStore>();

        //act
        var jobInfo = await service.AddJobAsync(jobRequest, default);

        //assert
        jobInfo.Should().NotBeNull();
        jobInfo.Description.Should().BeEquivalentTo(jobRequest.Description);
        jobInfo.JobWatchInterval.Should().Be(jobRequest.JobWatchInterval);
        jobInfo.JobParameters.Should().NotBeNull();
        jobInfo.CurrentState.Should().NotBeNull();
        jobInfo.JobType.Should().NotBeNull();
        jobInfo.MaxNumberOfTries.Should().Be(jobRequest.MaxNumberOfTries);
        jobInfo.CurrentNumberOfTries.Should().Be(jobRequest.NumberOfTries);

        repo.Verify(x => x.AddAsync(It.IsAny<JobEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_Store_Should_Set_Attempts()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var repo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        repo.Setup(x => x.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<JsonPatchDocument<JobEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobEntity());

        var service = fixture.Create<JobbaMongoJobStore>();

        //act
        var jobInfo = await service.SetJobAttempts<Foo, Foo>(Guid.NewGuid(), 2, default);

        //assert
        jobInfo.Should().NotBeNull();

        repo.Verify(x => x.UpdateAsync(
            It.IsAny<Guid>(),
            It.Is<JsonPatchDocument<JobEntity>>(patch => patch.Operations.Any(operation
                => operation.path == $"/{nameof(JobEntity.CurrentNumberOfTries)}" &&
                   (int)operation.value == 2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_Store_Should_Set_Job_Status()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var repo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        repo.Setup(x => x.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<JsonPatchDocument<JobEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobEntity());

        var service = fixture.Create<JobbaMongoJobStore>();
        var now = DateTimeOffset.UtcNow;

        //act
        await service.SetJobStatusAsync(Guid.NewGuid(), JobStatus.Completed, now, default);

        //assert

        repo.Verify(x => x.UpdateAsync(
            It.IsAny<Guid>(),
            It.Is<JsonPatchDocument<JobEntity>>(patch =>
                patch.Operations.Any(operation => operation.path == $"/{nameof(JobEntity.Status)}" && (JobStatus)operation.value == JobStatus.Completed) &&
                patch.Operations.Any(operation => operation.path == $"/{nameof(JobEntity.LastProgressDate)}" && (DateTimeOffset)operation.value == now)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_Store_Should_Log_Failure()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var repo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        repo.Setup(x => x.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<JsonPatchDocument<JobEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobEntity());

        var service = fixture.Create<JobbaMongoJobStore>();
        var ex = new Exception("fake");

        //act
        await service.LogFailureAsync(Guid.NewGuid(), ex, default);

        //assert

        repo.Verify(x => x.UpdateAsync(
            It.IsAny<Guid>(),
            It.Is<JsonPatchDocument<JobEntity>>(patch =>
                patch.Operations.Any(operation => operation.path == $"/{nameof(JobEntity.Status)}" && (JobStatus)operation.value == JobStatus.Faulted) &&
                patch.Operations.Any(operation => operation.path == $"/{nameof(JobEntity.FaultedReason)}" && (string)operation.value == ex.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_Store_Should_Get_Job_Info_Base_By_Id()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var jobId = Guid.NewGuid();

        var jobEntity = fixture.Create<JobEntity>();
        jobEntity.Id = jobId;

        var repo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        repo.Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<JobEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobEntity);

        var service = fixture.Create<JobbaMongoJobStore>();
        Expression<Func<JobEntity, bool>> expression = x => x.Id == jobId;

        //act
        var jobInfoBase = await service.GetJobByIdAsync(jobId, default);

        //assert
        repo.Verify(x => x.GetFirstOrDefaultAsync(
            It.Is<Expression<Func<JobEntity, bool>>>(exp => Lambda.ExpressionsEqual(exp, expression)),
            It.IsAny<CancellationToken>()), Times.Once);

        jobInfoBase.Should().NotBeNull();
        jobInfoBase.Id.Should().Be(jobId);
        jobInfoBase.Description.Should().Be(jobEntity.Description);
        jobInfoBase.Status.Should().Be(jobEntity.Status);
        jobInfoBase.FaultedReason.Should().Be(jobEntity.FaultedReason);
        jobInfoBase.EnqueuedTime.Should().Be(jobEntity.EnqueuedTime);
        jobInfoBase.JobType.Should().Be(jobEntity.JobType);
        jobInfoBase.JobWatchInterval.Should().Be(jobEntity.JobWatchInterval);
        jobInfoBase.LastProgressDate.Should().Be(jobEntity.LastProgressDate);
        jobInfoBase.CurrentNumberOfTries.Should().Be(jobEntity.CurrentNumberOfTries);
        jobInfoBase.MaxNumberOfTries.Should().Be(jobEntity.MaxNumberOfTries);
    }

    public class Foo
    {
    }
}
