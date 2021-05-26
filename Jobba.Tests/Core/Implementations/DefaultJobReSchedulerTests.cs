using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Events;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Implementations
{
    [TestClass]
    public class DefaultJobReSchedulerTests
    {
        [TestMethod]
        public async Task Default_Job_Re_Scheduler_Should_Publish_Event()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var jobIds = fixture.CreateMany<Guid>(5);
            var jobBases = jobIds.Select(x => new JobInfoBase {Id = x}).ToArray();


            var mockPublisher = fixture.Freeze<Mock<IJobEventPublisher>>();
            mockPublisher.Setup(
                x => x.PublishJobRestartEvent(It.IsAny<JobRestartEvent>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockJobStore = fixture.Freeze<Mock<IJobListStore>>();
            mockJobStore.Setup(x => x.GetJobsToRetry(It.IsAny<CancellationToken>())).ReturnsAsync(jobBases);

            var rescheduler = fixture.Create<DefaultJobReScheduler>();

            //act
            await rescheduler.RestartFaultedJobsAsync(default);

            //assert
            mockPublisher.Verify(
                x => x.PublishJobRestartEvent(It.IsAny<JobRestartEvent>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(5));
        }
    }
}
