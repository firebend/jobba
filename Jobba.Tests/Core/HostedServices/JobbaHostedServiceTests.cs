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

namespace Jobba.Tests.Core.HostedServices;

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

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object> { { typeof(IJobReScheduler), rescheduler.Object } }));

        var hostedService = fixture.Create<JobbaHostedService>();

        //act
        await hostedService.StartAsync(default);

        //assert
        rescheduler.Verify(x => x.RestartFaultedJobsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Jobba_Hosted_Service_Should_Cancel_Jobs_On_Exit()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var rescheduler = fixture.Freeze<Mock<IJobReScheduler>>();
        rescheduler.Setup(x => x.RestartFaultedJobsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jobCancellationStore = fixture.Freeze<Mock<IJobCancellationTokenStore>>();

        fixture.Customize(new ServiceProviderCustomization(new Dictionary<Type, object>
        {
            { typeof(IJobReScheduler), rescheduler.Object },
            { typeof(IJobCancellationTokenStore), jobCancellationStore.Object }
        }));

        var hostedService = fixture.Create<JobbaHostedService>();

        var tokenSource = new CancellationTokenSource();

        //act
        await hostedService.StartAsync(tokenSource.Token);
        await Task.Delay(TimeSpan.FromSeconds(1));
        tokenSource.Cancel();
        await Task.Delay(TimeSpan.FromSeconds(1));

        //assert
        rescheduler.Verify(x => x.RestartFaultedJobsAsync(It.IsAny<CancellationToken>()), Times.Once);
        jobCancellationStore.Verify(x => x.CancelAllJobs(), Times.Once);
    }
}
