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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neleus.LambdaCompare;

namespace Jobba.Tests.Mongo
{
    [TestClass]
    public class JobbaMongoJobListStoreTests
    {
        [TestMethod]
        public async Task Jobba_Mongo_Job_List_Store_Should_Get_Active_Jobs()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var mockRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
            mockRepo.Setup(x => x.GetAllAsync(
                It.IsAny<Expression<Func<JobEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.CreateMany<JobEntity>(5).ToList);

            var listStore = fixture.Create<JobbaMongoJobListStore>();
            Expression<Func<JobEntity, bool>> expectedExpression = x => x.Status == JobStatus.InProgress || x.Status == JobStatus.Enqueued;


            //act
            var activeJobs = await listStore.GetActiveJobs(default);

            //assert
            activeJobs.Count().Should().Be(5);

            mockRepo.Verify(x => x.GetAllAsync(
                    It.Is<Expression<Func<JobEntity, bool>>>(exp => Lambda.ExpressionsEqual(exp, expectedExpression)),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Jobba_Mongo_Job_List_Store_Should_Get_Jobs_To_Retry()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var mockRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
            mockRepo.Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<JobEntity, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.CreateMany<JobEntity>(5).ToList);

            var listStore = fixture.Create<JobbaMongoJobListStore>();
            Expression<Func<JobEntity, bool>> expectedExpression = x=> x.Status == JobStatus.Faulted && x.MaxNumberOfTries > x.CurrentNumberOfTries;


            //act
            var activeJobs = await listStore.GetJobsToRetry(default);

            //assert
            activeJobs.Count().Should().Be(5);

            mockRepo.Verify(x => x.GetAllAsync(
                It.Is<Expression<Func<JobEntity, bool>>>(exp => Lambda.ExpressionsEqual(exp, expectedExpression)),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
