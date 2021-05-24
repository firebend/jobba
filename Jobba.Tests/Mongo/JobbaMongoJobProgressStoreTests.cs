using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
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
            mockRepo.Setup(x => x.AddAsync(It.IsAny<JobProgressEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobProgressEntity());

            var service = fixture.Create<JobbaMongoJobProgressStore>();

            //act
            await service.LogProgressAsync(new JobProgress<object>(), default);

            //assert
            mockRepo.Verify(x => x.AddAsync(It.IsAny<JobProgressEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
