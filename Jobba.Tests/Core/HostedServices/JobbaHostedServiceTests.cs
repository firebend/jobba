using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.HostedServices;
using Jobba.Core.Interfaces;
using Jobba.Tests.AutoMoqCustomizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.HostedServices
{
    [TestClass]
    public class JobbaHostedServiceTests
    {
        [TestMethod]
        public async Task Jobba_Hosted_Service_Should_Execute()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var rescheduler = fixture.Freeze<Mock<IJobReScheduler>>();
            rescheduler.Setup(x => x.RestartFaultedJobsAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
            {
                {
                    typeof(IJobReScheduler), rescheduler.Object
                }
            }));

            var hostedService = fixture.Create<JobbaHostedService>();

            //act
            await hostedService.StartAsync(default);

            //assert
            rescheduler.Verify(x => x.RestartFaultedJobsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
