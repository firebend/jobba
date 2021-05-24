using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Abstractions
{
    [TestClass]
    public class AbstractJobBaseClassTests
    {
        private class JerbParameters
        {
            public string Param { get; set; }
        }

        private class JerbState
        {
            public string State { get; set; }
        }

        private class Jerb : AbstractJobBaseClass<JerbParameters,JerbState>
        {
            public Jerb(IJobProgressStore progressStore) : base(progressStore)
            {
            }

            protected override async Task OnStartAsync(JobStartContext<JerbParameters, JerbState> jobStartContext, CancellationToken cancellationToken)
                => await LogProgressAsync(new JerbState {State = "Fake State"}, 69.420m, "Fake Progress", cancellationToken);
        }

        [TestMethod]
        public async Task Abstract_Job_Base_Class_Should_Log_Progress()
        {
            //arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var mockProgressStore = fixture.Freeze<Mock<IJobProgressStore>>();
            mockProgressStore.Setup(x => x.LogProgressAsync(It.IsAny<JobProgress<JerbState>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var jerb = fixture.Create<Jerb>();

            //act
            await jerb.StartAsync(new JobStartContext<JerbParameters, JerbState>(), default);

            //assert
            mockProgressStore.Verify(x => x.LogProgressAsync(It.Is<JobProgress<JerbState>>(
                    progress => progress.Message == "Fake Progress" &&
                                progress.Progress == 69.420m &&
                                progress.Date > DateTimeOffset.MinValue &&
                                progress.JobState.State == "Fake State"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
