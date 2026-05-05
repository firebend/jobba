using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoCleanUpStoreTests
{
    [TestMethod]
    public async Task Jobba_Mongo_Clean_Up_Store_Should_Remove_Completed_Jobs()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var jobs = fixture.CreateMany<JobEntity>(5).ToList();

        var mockJobRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
        mockJobRepo.SetupSequence(x => x.DeleteManyAsync(
                It.IsAny<Expression<Func<JobEntity, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs)
            .ReturnsAsync([]);

        var mockJobProgressRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobProgressEntity>>>();
        mockJobProgressRepo.Setup(x => x.DeleteManyAsync(
                It.IsAny<Expression<Func<JobProgressEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.CreateMany<JobProgressEntity>(20).ToList);

        var sut = fixture.Create<JobbaMongoCleanUpStore>();

        //act
        await sut.CleanUpJobsAsync(TimeSpan.FromDays(60), 50, default);

        //assert
        mockJobRepo.Verify(x => x.DeleteManyAsync(
            It.IsAny<Expression<Func<JobEntity, bool>>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        mockJobProgressRepo.Verify(x => x.DeleteManyAsync(
            It.IsAny<Expression<Func<JobProgressEntity, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
