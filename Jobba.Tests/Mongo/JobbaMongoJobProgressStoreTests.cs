using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Mongo
{
    [TestClass]
    public class JobbaMongoJobProgressStoreTests
    {
        [TestMethod]
        public async Task Jobba_Mongo_Job_Progress_Store_Should_Add_Progress()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var mockRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobProgressEntity>>>();
            mockRepo.Setup(x => x.AddAsync(
                    It.IsAny<JobProgressEntity>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobProgressEntity());

            var mockJobRepo = fixture.Freeze<Mock<IJobbaMongoRepository<JobEntity>>>();
            mockJobRepo.Setup(x => x.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<JsonPatchDocument<JobEntity>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobEntity());

            var mockPublisher = fixture.Freeze<Mock<IJobEventPublisher>>();
            mockPublisher.Setup(x => x.PublishJobProgressEventAsync(
                    It.IsAny<JobProgressEvent>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = fixture.Create<JobbaMongoJobProgressStore>();

            //act
            await service.LogProgressAsync(new JobProgress<object>(), default);

            //assert
            mockRepo.Verify(x => x.AddAsync(
                It.IsAny<JobProgressEntity>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            mockPublisher.Verify(x => x.PublishJobProgressEventAsync(
                It.IsAny<JobProgressEvent>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            mockJobRepo.Verify(x => x.UpdateAsync(
                It.IsAny<Guid>(),
                It.Is<JsonPatchDocument<JobEntity>>(patch =>
                    patch.Operations.Any(o => o.path == "/JobState") &&
                    patch.Operations.Any(o => o.path == "/LastProgressDate") &&
                    patch.Operations.Any(o => o.path == "/LastProgressPercentage")),
                It.IsAny<CancellationToken>()));
        }
    }
}
