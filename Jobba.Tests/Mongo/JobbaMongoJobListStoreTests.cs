using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neleus.LambdaCompare;

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoJobListStoreTests
{
    private Fixture _fixture;
    private JobRegistration _jobRegistration;
    private Mock<IJobbaMongoRepository<JobEntity>> _mockRepo;

    [TestInitialize]
    public void TestSetup()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _jobRegistration = _fixture.JobRegistrationBuilder().Create();

        _fixture.Freeze<Mock<IJobSystemInfoProvider>>().Setup(x => x.GetSystemInfo())
            .Returns(TestModels.TestSystemInfo);
        _mockRepo = _fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        _mockRepo.Setup(x => x.GetAllAsync(
                It.IsAny<Expression<Func<JobEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.JobBuilder(_jobRegistration.Id).CreateMany(5).ToList)
            .Verifiable();
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_List_Store_Should_Get_Active_Jobs()
    {
        //arrange
        var listStore = _fixture.Create<JobbaMongoJobListStore>();
        var expectedExpression =
            RepositoryExpressions.JobsInProgressExpression(TestModels.TestSystemInfo);

        //act
        var activeJobs = await listStore.GetActiveJobs(default);

        //assert
        activeJobs.Count().Should().Be(5);

        _mockRepo.Verify(x => x.GetAllAsync(
            It.Is<Expression<Func<JobEntity, bool>>>(exp => Lambda.ExpressionsEqual(exp, expectedExpression)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Job_List_Store_Should_Get_Jobs_To_Retry()
    {
        //arrange
        var expectedExpression =
            RepositoryExpressions.JobRetryExpression(TestModels.TestSystemInfo);

        var listStore = _fixture.Create<JobbaMongoJobListStore>();

        //act
        var activeJobs = await listStore.GetJobsToRetry(default);

        //assert
        activeJobs.Count().Should().Be(5);

        _mockRepo.Verify(x => x.GetAllAsync(
            It.Is<Expression<Func<JobEntity, bool>>>(exp =>
                Lambda.ExpressionsEqual(exp, expectedExpression)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
