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

namespace Jobba.Tests.Mongo
{
    [TestClass]
    public class JobbaMongoJobStoreTests
    {
        public class Foo
        {

        }

        [TestMethod]
        public async Task Jobba_Mongo_Job_Store_Should_Add_Job()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var jobRequest = fixture.Create<JobRequest<Foo, Foo>>();
            var jobEntity = JobEntity.FromRequest(jobRequest);

            var repo = fixture.Freeze<Mock<IMongoJobRepository<JobEntity>>>();
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
    }
}
